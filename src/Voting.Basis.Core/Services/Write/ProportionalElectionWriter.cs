// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using DomainOfInfluence = Voting.Basis.Data.Models.DomainOfInfluence;
using ProportionalElection = Voting.Basis.Data.Models.ProportionalElection;
using ProportionalElectionCandidate = Voting.Basis.Data.Models.ProportionalElectionCandidate;
using ProportionalElectionList = Voting.Basis.Core.Domain.ProportionalElectionList;
using ProportionalElectionListUnion = Voting.Basis.Core.Domain.ProportionalElectionListUnion;
using ProportionalElectionUnion = Voting.Basis.Data.Models.ProportionalElectionUnion;

namespace Voting.Basis.Core.Services.Write;

public class ProportionalElectionWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly PoliticalBusinessValidationService _politicalBusinessValidationService;
    private readonly ContestValidationService _contestValidationService;
    private readonly IDbRepository<DataContext, ProportionalElection> _proportionalElectionRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidate> _proportionalElectionCandidateRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEntry> _proportionalElectionUnionEntryRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _proportionalElectionUnionRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly DomainOfInfluenceReader _doiReader;
    private readonly IAuth _auth;
    private readonly IDbRepository<DataContext, Data.Models.CantonSettings> _cantonSettingsRepo;

    public ProportionalElectionWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        PoliticalBusinessValidationService politicalBusinessValidationService,
        ContestValidationService contestValidationService,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IDbRepository<DataContext, ProportionalElectionCandidate> proportionalElectionCandidateRepo,
        IDbRepository<DataContext, ProportionalElectionUnionEntry> proportionalElectionUnionEntryRepo,
        IDbRepository<DataContext, ProportionalElectionUnion> proportionalElectionUnionRepo,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        DomainOfInfluenceReader doiReader,
        IAuth auth,
        IDbRepository<DataContext, Data.Models.CantonSettings> cantonSettingsRepo)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _politicalBusinessValidationService = politicalBusinessValidationService;
        _contestValidationService = contestValidationService;
        _proportionalElectionRepo = proportionalElectionRepo;
        _proportionalElectionCandidateRepo = proportionalElectionCandidateRepo;
        _proportionalElectionUnionEntryRepo = proportionalElectionUnionEntryRepo;
        _proportionalElectionUnionRepo = proportionalElectionUnionRepo;
        _doiRepo = doiRepo;
        _doiReader = doiReader;
        _auth = auth;
        _cantonSettingsRepo = cantonSettingsRepo;
    }

    public async Task Create(Domain.ProportionalElection data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(data.DomainOfInfluenceId);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            data.ReportDomainOfInfluenceLevel);
        await EnsureValidProportionalElectionMandateAlgorithm(data.MandateAlgorithm, data.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(data.ContestId);

        var proportionalElection = _aggregateFactory.New<ProportionalElectionAggregate>();

        proportionalElection.CreateFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task Update(Domain.ProportionalElection data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(data.DomainOfInfluenceId);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            data.ReportDomainOfInfluenceLevel);
        var contestState = await _contestValidationService.EnsureNotLocked(data.ContestId);

        var existingProportionalElection = await _proportionalElectionRepo.GetByKey(data.Id)
            ?? throw new EntityNotFoundException(nameof(Domain.ProportionalElection), data.Id);

        await EnsureValidProportionalElectionMandateAlgorithm(data.MandateAlgorithm, data.DomainOfInfluenceId);
        if (existingProportionalElection.ContestId != data.ContestId)
        {
            throw new ValidationException($"{nameof(existingProportionalElection.ContestId)} is immutable.");
        }

        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.Id);
        if (contestState.TestingPhaseEnded())
        {
            proportionalElection.UpdateAfterTestingPhaseEnded(data);
        }
        else
        {
            var hasPbUnions = await _proportionalElectionUnionEntryRepo
                .Query()
                .AnyAsync(x => x.ProportionalElectionId == data.Id);

            if (hasPbUnions && data.MandateAlgorithm != proportionalElection.MandateAlgorithm)
            {
                throw new ProportionalElectionEditMandateAlgorithmInUnionException();
            }

            proportionalElection.UpdateFrom(data);
        }

        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateActiveState(Guid proportionalElectionId, bool active)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateActiveState(active);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task Delete(Guid proportionalElectionId)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.Delete();
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task CreateList(ProportionalElectionList data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.CreateListFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateList(ProportionalElectionList data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        var contestState = await _contestValidationService.EnsureNotLocked(proportionalElection.ContestId);

        if (contestState.TestingPhaseEnded())
        {
            proportionalElection.UpdateListAfterTestingPhaseEnded(data);
        }
        else
        {
            proportionalElection.UpdateListFrom(data);
        }

        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task ReorderLists(Guid electionId, IReadOnlyCollection<EntityOrder> orders)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(electionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.ReorderLists(orders);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task DeleteList(Guid proportionalElectionListId)
    {
        var proportionalElection = await GetAggregateFromListId(proportionalElectionListId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.DeleteList(proportionalElectionListId);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task CreateListUnion(ProportionalElectionListUnion data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.CreateListUnionFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateListUnion(ProportionalElectionListUnion data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateListUnionFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateListUnionEntries(ProportionalElectionListUnionEntries data)
    {
        var proportionalElection = await GetAggregateFromListUnionId(data.ProportionalElectionListUnionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateListUnionEntriesFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateListUnionMainList(Guid unionId, Guid? mainListId)
    {
        var proportionalElection = await GetAggregateFromListUnionId(unionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateListUnionMainList(unionId, mainListId);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task ReorderListUnions(
        Guid electionId,
        Guid? rootListUnionId,
        IReadOnlyCollection<EntityOrder> orders)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(electionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.ReorderListUnions(rootListUnionId, orders);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task DeleteListUnion(Guid proportionalElectionListUnionId)
    {
        var proportionalElection = await GetAggregateFromListUnionId(proportionalElectionListUnionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.DeleteListUnion(proportionalElectionListUnionId);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task CreateCandidate(Domain.ProportionalElectionCandidate data)
    {
        var proportionalElection = await GetAggregateFromListId(data.ProportionalElectionListId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);
        var doi = await _doiRepo.GetByKey(proportionalElection.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(proportionalElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi);

        await EnsureValidParty(data, proportionalElection.DomainOfInfluenceId);

        proportionalElection.CreateCandidateFrom(data, candidateValidationParams);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateCandidate(Domain.ProportionalElectionCandidate data)
    {
        var proportionalElection = await GetAggregateFromListId(data.ProportionalElectionListId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        var contestState = await _contestValidationService.EnsureNotLocked(proportionalElection.ContestId);
        var doi = await _doiRepo.GetByKey(proportionalElection.DomainOfInfluenceId)
            ?? throw new EntityNotFoundException(proportionalElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi);

        await EnsureValidParty(
            data,
            proportionalElection.DomainOfInfluenceId,
            proportionalElection.FindCandidate(data.ProportionalElectionListId, data.Id).PartyId);

        if (contestState.TestingPhaseEnded())
        {
            proportionalElection.UpdateCandidateAfterTestingPhaseEnded(data, candidateValidationParams);
        }
        else
        {
            proportionalElection.UpdateCandidateFrom(data, candidateValidationParams);
        }

        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task ReorderCandidates(Guid proportionalElectionListId, IEnumerable<EntityOrder> orders)
    {
        var proportionalElection = await GetAggregateFromListId(proportionalElectionListId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.ReorderCandidates(proportionalElectionListId, orders);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task DeleteCandidate(Guid candidateId)
    {
        var candidate = await _proportionalElectionCandidateRepo.GetByKey(candidateId)
            ?? throw new ValidationException($"Proportional election candidate does not exist: {candidateId}");

        var proportionalElection = await GetAggregateFromListId(candidate.ProportionalElectionListId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.DeleteCandidate(candidate.ProportionalElectionListId, candidateId);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateAllMandateAlgorithmsInUnion(IReadOnlyCollection<Guid> unionIds, ProportionalElectionMandateAlgorithm mandateAlgorithm)
    {
        if (unionIds.Distinct().Count() != unionIds.Count)
        {
            throw new ValidationException("duplicate union id");
        }

        var unions = await _proportionalElectionUnionRepo.Query()
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.ProportionalElectionUnionEntries)
            .ThenInclude(x => x.ProportionalElection.DomainOfInfluence)
            .Where(x => unionIds.Contains(x.Id))
            .ToListAsync();

        var cantonSettingsList = await _cantonSettingsRepo.Query().ToListAsync();
        var tenantId = _auth.Tenant.Id;

        foreach (var union in unions)
        {
            _contestValidationService.EnsureInTestingPhase(union.Contest);

            foreach (var proportionalElection in union.ProportionalElectionUnionEntries.Select(x => x.ProportionalElection))
            {
                EnsureValidProportionalElectionMandateAlgorithm(mandateAlgorithm, proportionalElection.DomainOfInfluence!);

                var hasPermission = false;

                if (_auth.HasPermission(Permissions.PoliticalBusinessUnion.ActionsTenantSameCanton))
                {
                    var canton = union.Contest.DomainOfInfluence.Canton;
                    hasPermission = cantonSettingsList.Any(c => c.SecureConnectId == tenantId && c.Canton == canton);
                }
                else
                {
                    hasPermission = proportionalElection.DomainOfInfluence!.SecureConnectId == tenantId;
                }

                if (!hasPermission)
                {
                    throw new ForbiddenException("Insufficient permissions for the political business union update");
                }
            }
        }

        var proportionalElectionIds = unions.SelectMany(x => x.ProportionalElectionUnionEntries)
            .Select(x => x.ProportionalElectionId)
            .Distinct();

        var aggregates = new List<ProportionalElectionAggregate>();
        foreach (var proportionalElectionId in proportionalElectionIds)
        {
            var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);
            proportionalElection.UpdateMandatAlgorithm(mandateAlgorithm);
            aggregates.Add(proportionalElection);
        }

        foreach (var aggregate in aggregates)
        {
            await _aggregateRepository.Save(aggregate);
        }
    }

    internal async Task EnsureValidProportionalElectionMandateAlgorithm(
        ProportionalElectionMandateAlgorithm algo,
        Guid domainOfInfluenceId)
    {
        var doi = await _doiRepo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        EnsureValidProportionalElectionMandateAlgorithm(algo, doi);
    }

    private void EnsureValidProportionalElectionMandateAlgorithm(
        ProportionalElectionMandateAlgorithm algo,
        DomainOfInfluence doi)
    {
        if (!doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Contains(algo))
        {
            throw new ValidationException($"Canton settings does not allow proportional election mandate algorithm {algo}");
        }
    }

    private async Task<ProportionalElectionAggregate> GetAggregateFromListId(Guid listId)
    {
        var electionId = await _proportionalElectionRepo.Query()
            .Where(p => p.ProportionalElectionLists.Any(l => l.Id == listId))
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync()
            ?? throw new ValidationException($"Proportional election list does not exist: {listId}");

        return await _aggregateRepository.GetById<ProportionalElectionAggregate>(electionId);
    }

    private async Task<ProportionalElectionAggregate> GetAggregateFromListUnionId(Guid listUnionId)
    {
        var electionId = await _proportionalElectionRepo.Query()
            .Where(p => p.ProportionalElectionListUnions.Any(l => l.Id == listUnionId))
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync()
            ?? throw new ValidationException($"Proportional election list does not exist: {listUnionId}");

        return await _aggregateRepository.GetById<ProportionalElectionAggregate>(electionId);
    }

    private async Task EnsureValidParty(
        Domain.ProportionalElectionCandidate candidate,
        Guid doiId,
        Guid? previousPartyId = null)
    {
        if (!candidate.PartyId.HasValue)
        {
            throw new ValidationException($"{nameof(candidate.PartyId)} is required");
        }

        if (previousPartyId != null && candidate.PartyId == previousPartyId)
        {
            return;
        }

        await _doiReader.EnsurePartyExistsAndIsAccessibleByDomainOfInfluence(candidate.PartyId.Value, doiId);
    }
}
