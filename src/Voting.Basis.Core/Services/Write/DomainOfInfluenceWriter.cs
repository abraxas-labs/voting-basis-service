﻿// (c) Copyright by Abraxas Informatik AG
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
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Extensions;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Services;
using Voting.Lib.Iam.Store;
using Voting.Lib.MalwareScanner.Services;
using CountingCircle = Voting.Basis.Data.Models.CountingCircle;
using DomainOfInfluence = Voting.Basis.Data.Models.DomainOfInfluence;
using DomainOfInfluenceCountingCircle = Voting.Basis.Data.Models.DomainOfInfluenceCountingCircle;
using DomainOfInfluenceParty = Voting.Basis.Data.Models.DomainOfInfluenceParty;
using ExportConfiguration = Voting.Basis.Data.Models.ExportConfiguration;

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
    private readonly IDbRepository<DataContext, DomainOfInfluenceParty> _doiPartyRepo;
    private readonly IDbRepository<DataContext, ExportConfiguration> _exportConfigRepo;
    private readonly IAuth _auth;
    private readonly ITenantService _tenantService;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluenceLogoStorage _logoStorage;
    private readonly AppConfig _appConfig;
    private readonly IMalwareScannerService _malwareScannerService;
    private readonly DomainOfInfluenceCantonDefaultsBuilder _domainOfInfluenceCantonDefaultsBuilder;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly ContestDeleter _contestDeleter;
    private readonly PoliticalBusinessDeleter _politicalBusinessDeleter;

    public DomainOfInfluenceWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DomainOfInfluence> repo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluenceCountingCircle> doiCcRepo,
        IDbRepository<DataContext, DomainOfInfluenceParty> doiPartyRepo,
        IDbRepository<DataContext, ExportConfiguration> exportConfigRepo,
        IAuth auth,
        ITenantService tenantService,
        PermissionService permissionService,
        DomainOfInfluenceLogoStorage logoStorage,
        AppConfig appConfig,
        IMalwareScannerService malwareScannerService,
        DomainOfInfluenceCantonDefaultsBuilder domainOfInfluenceCantonDefaultsBuilder,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        ContestDeleter contestDeleter,
        PoliticalBusinessDeleter politicalBusinessDeleter)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _repo = repo;
        _countingCircleRepo = countingCircleRepo;
        _doiCcRepo = doiCcRepo;
        _doiPartyRepo = doiPartyRepo;
        _exportConfigRepo = exportConfigRepo;
        _auth = auth;
        _tenantService = tenantService;
        _permissionService = permissionService;
        _logoStorage = logoStorage;
        _appConfig = appConfig;
        _malwareScannerService = malwareScannerService;
        _domainOfInfluenceCantonDefaultsBuilder = domainOfInfluenceCantonDefaultsBuilder;
        _hierarchyRepo = hierarchyRepo;
        _contestDeleter = contestDeleter;
        _politicalBusinessDeleter = politicalBusinessDeleter;
    }

    public async Task Create(Domain.DomainOfInfluence data)
    {
        ValidateElectoralRegistration(data);
        var parent = await ValidateHierarchy(data);
        var cantonDefaults = await BuildCantonDefaults(data);

        await ValidateUniqueBfs(data);
        await SetAuthorityTenant(data);
        await ValidatePlausibilisationConfig(data, cantonDefaults.InternalPlausibilisationDisabled);
        await ValidateParties(null, data.Parties);
        await ValidateExportConfigurations(null, data.ExportConfigurations);
        await ValidateSuperiorAuthority(data, parent?.Canton ?? data.Canton);
        ValidatePublishResults(data, cantonDefaults.DomainOfInfluencePublishResultsOptionEnabled);
        await EnsureCanCreate(data, parent);

        var domainOfInfluence = _aggregateFactory.New<DomainOfInfluenceAggregate>();
        domainOfInfluence.CreateFrom(data);

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateForAdmin(Domain.DomainOfInfluence data)
    {
        var existingDoi = await _repo.GetByKey(data.Id)
            ?? throw new EntityNotFoundException(data.Id);
        await EnsureCanEdit(existingDoi.SecureConnectId, existingDoi.Canton, false);
        ValidateElectoralRegistration(data);
        var parent = await ValidateHierarchy(data);
        var cantonDefaults = existingDoi.CantonDefaults;
        await ValidateUniqueBfs(data);
        await SetAuthorityTenant(data);

        await ValidatePlausibilisationConfig(data, cantonDefaults.InternalPlausibilisationDisabled);
        await ValidateParties(data.Id, data.Parties);
        await ValidateExportConfigurations(data.Id, data.ExportConfigurations);

        await ValidateSuperiorAuthority(data, parent?.Canton ?? data.Canton);
        ValidatePublishResults(data, cantonDefaults.DomainOfInfluencePublishResultsOptionEnabled);

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(data.Id);

        if (_auth.HasPermission(Permissions.DomainOfInfluence.UpdateSameCanton)
            && domainOfInfluence.ParentId == null
            && data.Canton != domainOfInfluence.Canton)
        {
            throw new ForbiddenException("Not allowed to change the canton");
        }

        domainOfInfluence.UpdateFrom(data);

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateForElectionAdmin(Domain.DomainOfInfluence data)
    {
        var existingDoi = await _repo.GetByKey(data.Id)
            ?? throw new EntityNotFoundException(data.Id);
        var cantonDefaults = existingDoi.CantonDefaults;

        await ValidateUniqueBfs(data);
        await ValidatePlausibilisationConfig(data, cantonDefaults.InternalPlausibilisationDisabled);
        await ValidateParties(data.Id, data.Parties);
        await EnsureCanEdit(existingDoi.SecureConnectId, existingDoi.Canton);

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(data.Id);

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
        if (data.PlausibilisationConfiguration != null)
        {
            domainOfInfluence.UpdatePlausibilisationConfiguration(data.PlausibilisationConfiguration);
        }

        if (domainOfInfluence.ResponsibleForVotingCards)
        {
            domainOfInfluence.UpdateVotingCardData(
                data.ReturnAddress ?? throw new ValidationException(nameof(data.ReturnAddress) + " must be set"),
                data.PrintData ?? throw new ValidationException(nameof(data.PrintData) + " must be set"),
                data.ExternalPrintingCenter,
                data.ExternalPrintingCenterEaiMessageType,
                data.SapCustomerOrderNumber,
                null,
                data.VotingCardColor,
                data.StistatMunicipality,
                data.VotingCardFlatRateDisabled);
        }

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task Delete(Guid domainOfInfluenceId)
    {
        await EnsureCanDelete(domainOfInfluenceId);

        var idsToDelete = await _hierarchyRepo.Query()
            .Where(x => x.ParentIds.Contains(domainOfInfluenceId))
            .OrderByDescending(x => x.ParentIds.Count) // To make sure that we delete the lowest ones first
            .Select(x => x.DomainOfInfluenceId)
            .ToListAsync();
        idsToDelete.Add(domainOfInfluenceId);

        await _contestDeleter.DeleteInTestingPhase(idsToDelete);
        await _politicalBusinessDeleter.DeleteForDomainOfInfluencesInTestingPhase(idsToDelete);

        foreach (var id in idsToDelete)
        {
            var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(id);
            domainOfInfluence.Delete();

            // we keep the logo around, since we may want to use it again via history etc.
            await _aggregateRepository.Save(domainOfInfluence);
        }
    }

    public async Task UpdateDomainOfInfluenceCountingCircles(DomainOfInfluenceCountingCircleEntries data)
    {
        // Since non-root DOI aggregates do not keep track of the canton, we need to fetch that data from the database
        var dbDoi = await _repo.GetByKey(data.Id)
            ?? throw new EntityNotFoundException(data.Id);
        await EnsureCanEdit(dbDoi.SecureConnectId, dbDoi.Canton);
        await ValidateCountingCircles(dbDoi, data.CountingCircleIds);

        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(data.Id);
        domainOfInfluence.UpdateCountingCircleEntries(data);

        await _aggregateRepository.Save(domainOfInfluence);
    }

    public async Task UpdateLogo(Guid doiId, Stream logo, long logoLength, string? contentType, string? fileName, CancellationToken ct)
    {
        await EnsureCanEdit(doiId);
        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(doiId);

        // We cannot be sure that the logo stream supports seeking, so we copy the content (should not be large)
        using var logoContentStream = new MemoryStream();

        await logo.CopyToAsync(logoContentStream, ct);
        logoContentStream.Seek(0, SeekOrigin.Begin);

        await EnsureValidLogoContent(logoContentStream, contentType, fileName, ct);
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
        await EnsureCanEdit(id);
        var domainOfInfluence = await _aggregateRepository.GetById<DomainOfInfluenceAggregate>(id);

        var logoRef = domainOfInfluence.LogoRef;
        domainOfInfluence.DeleteLogo();
        await _aggregateRepository.Save(domainOfInfluence);

        await _logoStorage.Delete(logoRef!);
    }

    private async Task<DomainOfInfluence?> ValidateHierarchy(Domain.DomainOfInfluence data)
    {
        if (!data.ParentId.HasValue)
        {
            return null;
        }

        var parent = await _repo.GetByKey(data.ParentId.Value)
            ?? throw new EntityNotFoundException(nameof(Domain.DomainOfInfluence), data.ParentId);

        if (data.Type.IsPolitical() && parent.Type.IsPolitical() && data.Type - parent.Type < 0)
        {
            throw new ValidationException("Violate political hierarchical order");
        }

        return parent;
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
            .AnyAsync(x => x.Type == DomainOfInfluenceType.Mu && x.Bfs == data.Bfs && x.Id != data.Id);
        if (anotherExists)
        {
            throw new DuplicatedBfsException(data.Bfs);
        }
    }

    private async Task ValidateCountingCircles(DomainOfInfluence domainOfInfluence, IReadOnlyCollection<Guid> countingCircleIds)
    {
        if (countingCircleIds.Count == 0)
        {
            return;
        }

        var query = _countingCircleRepo.Query();

        if (_auth.HasPermission(Permissions.DomainOfInfluenceHierarchy.UpdateSameCanton))
        {
            query = query.Where(x => x.Canton == domainOfInfluence.Canton);
        }

        var foundCircleIds = await query
            .Select(cc => cc.Id)
            .Where(ccId => countingCircleIds.Contains(ccId)) // Intersect() in a way ef core can translate it
            .ToListAsync();

        if (foundCircleIds.Count != countingCircleIds.Count)
        {
            var missingId = countingCircleIds.Except(foundCircleIds).First();
            throw new EntityNotFoundException(missingId);
        }

        await EnsureCountingCircleIsNotAlreadyInheritedInTree(domainOfInfluence.Id, countingCircleIds);
    }

    private async Task SetAuthorityTenant(Domain.DomainOfInfluence data)
    {
        var tenant = await _tenantService.GetTenant(data.SecureConnectId, true)
            ?? throw new ValidationException($"tenant with id {data.SecureConnectId} not found");
        data.AuthorityName = tenant.Name;
    }

    private async Task EnsureCountingCircleIsNotAlreadyInheritedInTree(Guid domainOfInfluenceId, IReadOnlyCollection<Guid> countingCircleIds)
    {
        // validation to prevent events with inherited CcIds
        var inheritedCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == domainOfInfluenceId)
            .WhereIsInherited()
            .Select(doiCc => doiCc.CountingCircleId)
            .Distinct()
            .ToListAsync();

        if (inheritedCcIds.Any(inheritedDoiCcId => countingCircleIds.Contains(inheritedDoiCcId)))
        {
            throw new ValidationException("A CountingCircle cannot be added if he is already inherited in the DomainOfInfluence Tree");
        }
    }

    private async Task ValidatePlausibilisationConfig(Domain.DomainOfInfluence doi, bool internalPlausibilisationDisabled)
    {
        if (doi.PlausibilisationConfiguration == null)
        {
            if (!internalPlausibilisationDisabled)
            {
                throw new ValidationException("plausibilisation configuration is required");
            }

            return;
        }

        if (internalPlausibilisationDisabled)
        {
            throw new ValidationException("internal plausibilisation is disabled for this canton");
        }

        var isNew = doi.Id == Guid.Empty;
        var ccEntries = doi.PlausibilisationConfiguration.ComparisonCountOfVotersCountingCircleEntries;
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
            .Where(doiCc => doiCc.DomainOfInfluenceId == doi.Id)
            .Select(doiCc => doiCc.CountingCircleId)
            .Distinct()
            .ToListAsync();

        if (ccIds.Any(ccId => !doiCcIds.Contains(ccId)))
        {
            throw new ValidationException("A counting circle must be assigned to the domain of influence");
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

    private async Task ValidateExportConfigurations(Guid? doiId, IReadOnlyCollection<Domain.ExportConfiguration> configurations)
    {
        if (configurations.Count == 0)
        {
            return;
        }

        var configIds = configurations.Select(x => x.Id).ToHashSet();
        var hasUnallowedConfig = await _exportConfigRepo.Query()
            .AnyAsync(c => configIds.Contains(c.Id) && (doiId == null || c.DomainOfInfluenceId != doiId));

        if (hasUnallowedConfig)
        {
            throw new ValidationException("Some export configurations cannot be modified because they do not belong to the domain of influence");
        }
    }

    private void ValidateElectoralRegistration(Domain.DomainOfInfluence data)
    {
        if (!data.ResponsibleForVotingCards && data.ElectoralRegistrationEnabled)
        {
            throw new ValidationException(
                "Domain of influence needs to be 'responsible for voting cards' in order to be able to use the electoral registration.");
        }

        if (!data.ElectoralRegistrationEnabled && data.ElectoralRegisterMultipleEnabled)
        {
            throw new ValidationException(
                "Domain of influence needs to have enabled electoral registration to be able to use multiple electoral registers.");
        }
    }

    private async Task EnsureValidLogoContent(Stream contentStream, [NotNull] string? mimeType, [NotNull] string? fileName, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(mimeType) || string.IsNullOrEmpty(fileName))
        {
            throw new FluentValidation.ValidationException("Both the MIME type (content type) and the file name must be provided");
        }

        await _malwareScannerService.EnsureFileIsClean(contentStream, ct);
        var extensionFromFileName = Path.GetExtension(fileName)
            .TrimStart(LeadingFileExtensionChar)
            .ToLowerInvariant();

        var extensionFromReceivedMimeType = MimeTypesMap.GetExtension(mimeType.Split(ContentTypeCharsetSeparator)[0]);
        contentStream.Seek(0, SeekOrigin.Begin);
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

    private async Task EnsureCanCreate(Domain.DomainOfInfluence domainOfInfluence, DomainOfInfluence? parent)
    {
        // Either this is a root DOI with the same canton or the parent has the same canton
        var canton = parent?.Canton ?? domainOfInfluence.Canton;
        if (await _permissionService.IsOwnerOfCanton(canton))
        {
            return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureCanEdit(Guid domainOfInfluenceId, bool allowSameTenant = true)
    {
        // Since non-root DOI aggregates do not keep track of the canton, we need to fetch that data from the database
        var dbDoi = await _repo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);
        await EnsureCanEdit(dbDoi.SecureConnectId, dbDoi.Canton, allowSameTenant);
    }

    private async Task EnsureCanEdit(string doiTenantId, DomainOfInfluenceCanton canton, bool allowSameTenant = true)
    {
        if (allowSameTenant && _auth.HasPermission(Permissions.DomainOfInfluence.UpdateSameTenant) && _auth.Tenant.Id == doiTenantId)
        {
            return;
        }

        if (_auth.HasPermission(Permissions.DomainOfInfluence.UpdateSameCanton) && await _permissionService.IsOwnerOfCanton(canton))
        {
            return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureCanDelete(Guid domainOfInfluenceId)
    {
        // Since non-root DOI aggregates do not keep track of the canton, we need to fetch that data from the database
        var doi = await _repo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        if (_auth.HasPermission(Permissions.DomainOfInfluence.DeleteSameCanton) && await _permissionService.IsOwnerOfCanton(doi.Canton))
        {
            return;
        }

        throw new ForbiddenException();
    }

    private async Task<DomainOfInfluenceCantonDefaults> BuildCantonDefaults(Domain.DomainOfInfluence doi)
    {
        if (doi.Canton == DomainOfInfluenceCanton.Unspecified && doi.ParentId == null)
        {
            throw new ValidationException("canton is required to load canton settings for root doi");
        }

        var cantonSettings = await _domainOfInfluenceCantonDefaultsBuilder.LoadCantonSettings(doi.Canton, doi.ParentId);
        return _domainOfInfluenceCantonDefaultsBuilder.BuildCantonDefaults(cantonSettings, doi.Type);
    }

    private async Task ValidateSuperiorAuthority(Domain.DomainOfInfluence doi, DomainOfInfluenceCanton canton)
    {
        if (!doi.SuperiorAuthorityDomainOfInfluenceId.HasValue)
        {
            return;
        }

        if (canton == DomainOfInfluenceCanton.Unspecified)
        {
            throw new ArgumentException($"Canton must not be {nameof(DomainOfInfluenceCanton.Unspecified)}");
        }

        var superiorAuthorityDoi = await _repo.GetByKey(doi.SuperiorAuthorityDomainOfInfluenceId.Value)
            ?? throw new EntityNotFoundException(doi.SuperiorAuthorityDomainOfInfluenceId);

        if (superiorAuthorityDoi.Canton != canton)
        {
            throw new ValidationException("Cannot set a domain of influence from a different canton as superior authority");
        }

        if (superiorAuthorityDoi.Type > DomainOfInfluenceType.Mu)
        {
            throw new ValidationException("The selected superior authority domain of influence has an invalid type");
        }
    }

    private void ValidatePublishResults(Domain.DomainOfInfluence doi, bool domainOfInfluencePublishResultsOptionEnabled)
    {
        if (!doi.PublishResultsDisabled)
        {
            return;
        }

        if (!domainOfInfluencePublishResultsOptionEnabled)
        {
            throw new ValidationException("Canton does not allow to disable publish results");
        }

        if (!doi.Type.IsCommunal())
        {
            throw new ValidationException("Cannot disable publish results on non-communal domain of influence");
        }
    }
}
