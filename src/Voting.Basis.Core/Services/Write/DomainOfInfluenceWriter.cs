// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyRed.Mime;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.ObjectStorage;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Services;
using Voting.Lib.Iam.Store;
using CountingCircle = Voting.Basis.Data.Models.CountingCircle;
using DomainOfInfluence = Voting.Basis.Data.Models.DomainOfInfluence;
using DomainOfInfluenceCountingCircle = Voting.Basis.Data.Models.DomainOfInfluenceCountingCircle;
using DomainOfInfluenceParty = Voting.Basis.Data.Models.DomainOfInfluenceParty;
using PlausibilisationConfiguration = Voting.Basis.Core.Domain.PlausibilisationConfiguration;

namespace Voting.Basis.Core.Services.Write;

public class DomainOfInfluenceWriter
{
    private const char LeadingFileExtensionChar = '.';
    private const char ContentTypeCharsetSeparator = ';';
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _repo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceCountingCircle> _doiCcRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceParty> _doiPartyRepo;
    private readonly IAuth _auth;
    private readonly ITenantService _tenantService;
    private readonly DomainOfInfluenceLogoStorage _logoStorage;
    private readonly AppConfig _appConfig;

    public DomainOfInfluenceWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DomainOfInfluence> repo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluenceCountingCircle> doiCcRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        IDbRepository<DataContext, DomainOfInfluenceParty> doiPartyRepo,
        IAuth auth,
        ITenantService tenantService,
        DomainOfInfluenceLogoStorage logoStorage,
        AppConfig appConfig)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _repo = repo;
        _countingCircleRepo = countingCircleRepo;
        _doiCcRepo = doiCcRepo;
        _hierarchyRepo = hierarchyRepo;
        _doiPartyRepo = doiPartyRepo;
        _auth = auth;
        _tenantService = tenantService;
        _logoStorage = logoStorage;
        _appConfig = appConfig;
    }

    public async Task Create(Domain.DomainOfInfluence data)
    {
        _auth.EnsureAdmin();
        await ValidateHierarchy(data);
        await ValidateUniqueBfs(data);
        await SetAuthorityTenant(data);
        await ValidatePlausibilisationConfig(null, data.PlausibilisationConfiguration);
        await ValidateParties(null, data.Parties);

        var domainOfInfluence = _aggregateFactory.New<DomainOfInfluenceAggregate>();
        domainOfInfluence.CreateFrom(data);

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateForAdmin(Domain.DomainOfInfluence data)
    {
        _auth.EnsureAdmin();
        await ValidateHierarchy(data);
        await ValidateUniqueBfs(data);
        await SetAuthorityTenant(data);

        await ValidatePlausibilisationConfig(data.Id, data.PlausibilisationConfiguration);
        await ValidateParties(data.Id, data.Parties);

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(data.Id);
        domainOfInfluence.UpdateFrom(data);

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateForElectionAdmin(Domain.DomainOfInfluence data)
    {
        _auth.EnsureAdminOrElectionAdmin();

        await ValidateUniqueBfs(data);
        await ValidatePlausibilisationConfig(data.Id, data.PlausibilisationConfiguration);
        await ValidateParties(data.Id, data.Parties);

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(data.Id);
        EnsureIsAdminOrResponsibleTenant(domainOfInfluence);

        if (data.PlausibilisationConfiguration == null)
        {
            throw new ValidationException("PlausibilisationConfiguration must not be null");
        }

        if (data.ContactPerson == null)
        {
            throw new ValidationException("ContactPerson must not be null");
        }

        if (domainOfInfluence.ResponsibleForVotingCards && (data.ReturnAddress == null || data.PrintData == null))
        {
            throw new ValidationException("ReturnAddress and PrintData must not be null");
        }

        domainOfInfluence.UpdateContactPerson(data.ContactPerson);
        domainOfInfluence.UpdateParties(data.Parties);
        domainOfInfluence.UpdatePlausibilisationConfiguration(data.PlausibilisationConfiguration);

        if (domainOfInfluence.ResponsibleForVotingCards)
        {
            domainOfInfluence.UpdateVotingCardData(
                data.ReturnAddress ?? throw new ValidationException(nameof(data.ReturnAddress) + " must be set"),
                data.PrintData ?? throw new ValidationException(nameof(data.PrintData) + " must be set"),
                data.ExternalPrintingCenter,
                data.ExternalPrintingCenterEaiMessageType);
        }

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task Delete(Guid domainOfInfluenceId)
    {
        _auth.EnsureAdmin();

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(domainOfInfluenceId);
        domainOfInfluence.Delete();

        // we keep the logo around, since we may want to use it again via history etc.
        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateDomainOfInfluenceCountingCircles(DomainOfInfluenceCountingCircleEntries data)
    {
        _auth.EnsureAdmin();

        await ValidateCountingCircles(data.Id, data.CountingCircleIds);

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(data.Id);
        domainOfInfluence.UpdateCountingCircleEntries(data);

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateLogo(Guid doiId, Stream logo, long logoLength, string? contentType, string? fileName, CancellationToken ct)
    {
        _auth.EnsureAdminOrElectionAdmin();
        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(doiId);
        EnsureIsAdminOrResponsibleTenant(domainOfInfluence);

        // We cannot be sure that the logo stream supports seeking, so we copy the content (should not be large)
        using var logoContentStream = new MemoryStream();
        await logo.CopyToAsync(logoContentStream, ct);
        logoContentStream.Seek(0, SeekOrigin.Begin);

        EnsureValidLogoContent(logoContentStream, contentType, fileName);
        logoContentStream.Seek(0, SeekOrigin.Begin);

        domainOfInfluence.UpdateLogo();
        await _logoStorage.Store(
            domainOfInfluence.Id,
            domainOfInfluence.LogoRef!,
            logoContentStream,
            logoLength,
            contentType,
            ct);
        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task DeleteLogo(Guid id)
    {
        _auth.EnsureAdminOrElectionAdmin();
        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(id);
        EnsureIsAdminOrResponsibleTenant(domainOfInfluence);

        var logoRef = domainOfInfluence.LogoRef;
        domainOfInfluence.DeleteLogo();
        await _aggregateRepository.Save(domainOfInfluence);

        await _logoStorage.Delete(logoRef!);
    }

    private async Task ValidateHierarchy(Domain.DomainOfInfluence data)
    {
        if (!data.ParentId.HasValue)
        {
            return;
        }

        var parent = await _repo.GetByKey(data.ParentId.Value)
            ?? throw new EntityNotFoundException(nameof(Domain.DomainOfInfluence), data.ParentId);

        var selfIsPolitical = data.Type.IsPolitical();
        var parentIsPolitical = parent.Type.IsPolitical();

        if (parentIsPolitical && !selfIsPolitical)
        {
            throw new ValidationException("Non political DomainOfInfluence may only be a child of a non political DomainOfInfluence Parent");
        }

        if (!parentIsPolitical && selfIsPolitical)
        {
            throw new ValidationException("Political DomainOfInfluence may only be a child of a political DomainOfInfluence Parent");
        }

        if (selfIsPolitical && data.Type - parent.Type < 0)
        {
            throw new ValidationException("Violate political hierarchical order");
        }
    }

    private async Task ValidateUniqueBfs(Domain.DomainOfInfluence data)
    {
        // only mu's are validated
        if (data.Type != DomainOfInfluenceType.Mu)
        {
            return;
        }

        data.Bfs = data.Bfs.Trim();
        if (data.Bfs.Length == 0)
        {
            throw new ValidationException("Bfs is required for Mu domain of influences");
        }

        var anotherExists = await _repo
            .Query()
            .AnyAsync(x => x.Bfs == data.Bfs && x.Id != data.Id);
        if (anotherExists)
        {
            throw new DuplicatedBfsException(data.Bfs);
        }
    }

    private async Task ValidateCountingCircles(Guid domainOfInfluenceId, IReadOnlyCollection<Guid> countingCircleIds)
    {
        if (countingCircleIds.Count == 0)
        {
            return;
        }

        var foundCircleIds = await _countingCircleRepo.Query()
            .Select(cc => cc.Id)
            .Where(ccId => countingCircleIds.Contains(ccId)) // Intersect() in a way ef core can translate it
            .ToListAsync();

        if (foundCircleIds.Count != countingCircleIds.Count)
        {
            var missingId = countingCircleIds.Except(foundCircleIds).First();
            throw new EntityNotFoundException(missingId);
        }

        await EnsureCountingCircleOnlyOnceInTree(domainOfInfluenceId, countingCircleIds);
    }

    private async Task SetAuthorityTenant(Domain.DomainOfInfluence data)
    {
        var tenant = await _tenantService.GetTenant(data.SecureConnectId, true)
                     ?? throw new ValidationException(
                         $"tenant with id {data.SecureConnectId} not found");
        data.AuthorityName = tenant.Name;
    }

    private async Task EnsureCountingCircleOnlyOnceInTree(Guid domainOfInfluenceId, IReadOnlyCollection<Guid> countingCircleIds)
    {
        // validation with nonInheritedCcIds to prevent events with inherited CcIds
        var nonInheritedCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == domainOfInfluenceId && !doiCc.Inherited)
            .Select(doiCc => doiCc.CountingCircleId)
            .ToListAsync();

        var ccIdsToAdd = countingCircleIds.Except(nonInheritedCcIds).ToList();

        var doiHierarchy = await _hierarchyRepo
            .Query()
            .FirstOrDefaultAsync(h => h.DomainOfInfluenceId == domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        // the root doi contains all ccIds of the tree
        var rootDoiCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == doiHierarchy.RootId)
            .Select(doiCc => doiCc.CountingCircleId)
            .ToListAsync();

        if (rootDoiCcIds.Any(rootDoiCcId => ccIdsToAdd.Contains(rootDoiCcId)))
        {
            throw new ValidationException("A CountingCircle cannot be added twice in the same DomainOfInfluence Tree");
        }
    }

    private async Task ValidatePlausibilisationConfig(
        Guid? doiId,
        PlausibilisationConfiguration? plausiConfig)
    {
        if (plausiConfig == null)
        {
            return;
        }

        var isNew = doiId == null;
        var ccEntries = plausiConfig.ComparisonCountOfVotersCountingCircleEntries;
        if (isNew && ccEntries.Count > 0)
        {
            throw new ValidationException("Comparison count of voters counting circle entries only allowed on update");
        }

        if (isNew || ccEntries.Count == 0)
        {
            return;
        }

        var ccIds = ccEntries
            .Select(x => x.CountingCircleId)
            .ToHashSet();

        var doiCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == doiId)
            .Select(doiCc => doiCc.CountingCircleId)
            .ToListAsync();

        if (ccIds.Any(ccId => !doiCcIds.Contains(ccId)))
        {
            throw new ValidationException("A counting circle must be assigned to the domain of influence");
        }
    }

    private void EnsureIsAdminOrResponsibleTenant(DomainOfInfluenceAggregate doi)
    {
        if (!_auth.IsAdmin() && !_auth.Tenant.Id.Equals(doi.SecureConnectId, StringComparison.Ordinal))
        {
            throw new ForbiddenException();
        }
    }

    private async Task ValidateParties(Guid? doiId, IReadOnlyCollection<Domain.DomainOfInfluenceParty> parties)
    {
        if (parties.Count == 0)
        {
            return;
        }

        var partyIds = parties.Select(x => x.Id).ToHashSet();
        var hasUnallowedDoiParty = await _doiPartyRepo.Query()
            .AnyAsync(p => partyIds.Contains(p.Id) && (doiId == null || p.DomainOfInfluenceId != doiId));

        if (hasUnallowedDoiParty)
        {
            throw new ValidationException("Some parties cannot be modified because they do not belong to the domain of influence");
        }
    }

    private void EnsureValidLogoContent(Stream contentStream, [NotNull] string? mimeType, [NotNull] string? fileName)
    {
        if (string.IsNullOrEmpty(mimeType) || string.IsNullOrEmpty(fileName))
        {
            throw new FluentValidation.ValidationException("Both the MIME type (content type) and the file name must be provided");
        }

        var extensionFromFileName = Path.GetExtension(fileName)
            .TrimStart(LeadingFileExtensionChar)
            .ToLowerInvariant();

        var extensionFromReceivedMimeType = MimeTypesMap.GetExtension(mimeType.Split(ContentTypeCharsetSeparator)[0]);

        var guessedMimeType = MimeGuesser.GuessMimeType(contentStream);
        var extensionFromGuessedMimeType = MimeTypesMap.GetExtension(guessedMimeType);

        if (extensionFromFileName != extensionFromReceivedMimeType || extensionFromFileName != extensionFromGuessedMimeType)
        {
            throw new ValidationException(
                $"File extensions differ. From file name: {extensionFromFileName}, from content type: {extensionFromReceivedMimeType}, guessed from content: {extensionFromGuessedMimeType}");
        }

        if (!_appConfig.Publisher.AllowedLogoFileExtensions.Contains(extensionFromFileName))
        {
            throw new ValidationException($"File extension {extensionFromFileName} is not allowed for logo uploads");
        }
    }
}
