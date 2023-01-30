// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DomainOfInfluence = Voting.Basis.Data.Models.DomainOfInfluence;
using ProportionalElection = Voting.Basis.Data.Models.ProportionalElection;
using ProportionalElectionCandidate = Voting.Basis.Data.Models.ProportionalElectionCandidate;
using ProportionalElectionList = Voting.Basis.Core.Domain.ProportionalElectionList;
using ProportionalElectionListUnion = Voting.Basis.Core.Domain.ProportionalElectionListUnion;

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
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly DomainOfInfluenceReader _doiReader;

    public ProportionalElectionWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        PoliticalBusinessValidationService politicalBusinessValidationService,
        ContestValidationService contestValidationService,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IDbRepository<DataContext, ProportionalElectionCandidate> proportionalElectionCandidateRepo,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        DomainOfInfluenceReader doiReader)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _politicalBusinessValidationService = politicalBusinessValidationService;
        _contestValidationService = contestValidationService;
        _proportionalElectionRepo = proportionalElectionRepo;
        _proportionalElectionCandidateRepo = proportionalElectionCandidateRepo;
        _doiRepo = doiRepo;
        _doiReader = doiReader;
    }

    public async Task Create(Domain.ProportionalElection data)
    {
        await _permissionService.EnsureCanModifyPoliticalBusiness(data.DomainOfInfluenceId);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.ContestId,
            data.DomainOfInfluenceId);
        await EnsureValidProportionalElectionMandateAlgorithm(data.MandateAlgorithm, data.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(data.ContestId);

        var proportionalElection = _aggregateFactory.New<ProportionalElectionAggregate>();

        proportionalElection.CreateFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task Update(Domain.ProportionalElection data)
    {
        await _permissionService.EnsureCanModifyPoliticalBusiness(data.DomainOfInfluenceId);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.ContestId,
            data.DomainOfInfluenceId);
        var contestState = await _contestValidationService.EnsureNotLocked(data.ContestId);

        var existingProportionalElection = await _proportionalElectionRepo.GetByKey(data.Id)
            ?? throw new EntityNotFoundException(nameof(Domain.ProportionalElection), data.Id);

        await EnsureValidProportionalElectionMandateAlgorithm(data.MandateAlgorithm, data.DomainOfInfluenceId, existingProportionalElection.MandateAlgorithm);
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
            proportionalElection.UpdateFrom(data);
        }

        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateActiveState(Guid proportionalElectionId, bool active)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);

        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateActiveState(active);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task Delete(Guid proportionalElectionId)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);

        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.Delete();
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task CreateList(ProportionalElectionList data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);

        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.CreateListFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateList(ProportionalElectionList data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
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
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.ReorderLists(orders);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task DeleteList(Guid proportionalElectionListId)
    {
        var proportionalElection = await GetAggregateFromListId(proportionalElectionListId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.DeleteList(proportionalElectionListId);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task CreateListUnion(ProportionalElectionListUnion data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.CreateListUnionFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateListUnion(ProportionalElectionListUnion data)
    {
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(data.ProportionalElectionId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateListUnionFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateListUnionEntries(ProportionalElectionListUnionEntries data)
    {
        var proportionalElection = await GetAggregateFromListUnionId(data.ProportionalElectionListUnionId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.UpdateListUnionEntriesFrom(data);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateListUnionMainList(Guid unionId, Guid? mainListId)
    {
        var proportionalElection = await GetAggregateFromListUnionId(unionId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
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
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.ReorderListUnions(rootListUnionId, orders);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task DeleteListUnion(Guid proportionalElectionListUnionId)
    {
        var proportionalElection = await GetAggregateFromListUnionId(proportionalElectionListUnionId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.DeleteListUnion(proportionalElectionListUnionId);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task CreateCandidate(Domain.ProportionalElectionCandidate data)
    {
        var proportionalElection = await GetAggregateFromListId(data.ProportionalElectionListId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);
        var doi = await _doiRepo.GetByKey(proportionalElection.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(proportionalElection.DomainOfInfluenceId);

        await EnsureValidParty(data, proportionalElection.DomainOfInfluenceId);

        proportionalElection.CreateCandidateFrom(data, doi.Type);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task UpdateCandidate(Domain.ProportionalElectionCandidate data)
    {
        var proportionalElection = await GetAggregateFromListId(data.ProportionalElectionListId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        var contestState = await _contestValidationService.EnsureNotLocked(proportionalElection.ContestId);
        var doi = await _doiRepo.GetByKey(proportionalElection.DomainOfInfluenceId)
            ?? throw new EntityNotFoundException(proportionalElection.DomainOfInfluenceId);

        await EnsureValidParty(
            data,
            proportionalElection.DomainOfInfluenceId,
            proportionalElection.FindCandidate(data.ProportionalElectionListId, data.Id).PartyId);

        if (contestState.TestingPhaseEnded())
        {
            proportionalElection.UpdateCandidateAfterTestingPhaseEnded(data, doi.Type);
        }
        else
        {
            proportionalElection.UpdateCandidateFrom(data, doi.Type);
        }

        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task ReorderCandidates(Guid proportionalElectionListId, IEnumerable<EntityOrder> orders)
    {
        var proportionalElection = await GetAggregateFromListId(proportionalElectionListId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.ReorderCandidates(proportionalElectionListId, orders);
        await _aggregateRepository.Save(proportionalElection);
    }

    public async Task DeleteCandidate(Guid candidateId)
    {
        var candidate = await _proportionalElectionCandidateRepo.GetByKey(candidateId)
            ?? throw new ValidationException($"Proportional election candidate does not exist: {candidateId}");

        var proportionalElection = await GetAggregateFromListId(candidate.ProportionalElectionListId);
        await _permissionService.EnsureCanModifyPoliticalBusiness(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        proportionalElection.DeleteCandidate(candidate.ProportionalElectionListId, candidateId);
        await _aggregateRepository.Save(proportionalElection);
    }

    internal async Task EnsureValidProportionalElectionMandateAlgorithm(
        ProportionalElectionMandateAlgorithm algo,
        Guid politicalBusinessDomainOfInfluenceId,
        ProportionalElectionMandateAlgorithm? existingAlgo = null)
    {
        if (existingAlgo.HasValue && existingAlgo != algo)
        {
            return;
        }

        var doi = await _doiRepo.GetByKey(politicalBusinessDomainOfInfluenceId)
            ?? throw new EntityNotFoundException(politicalBusinessDomainOfInfluenceId);

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
