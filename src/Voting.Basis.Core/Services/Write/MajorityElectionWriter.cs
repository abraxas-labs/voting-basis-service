// (c) Copyright by Abraxas Informatik AG
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
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DomainOfInfluence = Voting.Basis.Data.Models.DomainOfInfluence;
using ElectionGroup = Voting.Basis.Data.Models.ElectionGroup;
using MajorityElection = Voting.Basis.Data.Models.MajorityElection;
using MajorityElectionBallotGroup = Voting.Basis.Data.Models.MajorityElectionBallotGroup;
using MajorityElectionCandidate = Voting.Basis.Data.Models.MajorityElectionCandidate;
using SecondaryMajorityElection = Voting.Basis.Data.Models.SecondaryMajorityElection;

namespace Voting.Basis.Core.Services.Write;

public class MajorityElectionWriter : PoliticalBusinessWriter
{
    private const string DefaultElectionGroupDescriptionPrefix = "Wahlgruppe";
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly PoliticalBusinessValidationService _politicalBusinessValidationService;
    private readonly ContestValidationService _contestValidationService;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _majorityElectionCandidateRepo;
    private readonly IDbRepository<DataContext, ElectionGroup> _electionGroupRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryMajorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroup> _ballotGroupRepo;

    public MajorityElectionWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        PoliticalBusinessValidationService politicalBusinessValidationService,
        ContestValidationService contestValidationService,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionCandidate> majorityElectionCandidateRepo,
        IDbRepository<DataContext, ElectionGroup> electionGroupRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryMajorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionBallotGroup> ballotGroupRepo)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _politicalBusinessValidationService = politicalBusinessValidationService;
        _contestValidationService = contestValidationService;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _majorityElectionCandidateRepo = majorityElectionCandidateRepo;
        _electionGroupRepo = electionGroupRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _ballotGroupRepo = ballotGroupRepo;
    }

    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public async Task Create(Domain.MajorityElection data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(data.DomainOfInfluenceId, false);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            PoliticalBusinessType.MajorityElection,
            data.ReportDomainOfInfluenceLevel);
        await _contestValidationService.EnsureInTestingPhase(data.ContestId);

        var majorityElection = _aggregateFactory.New<MajorityElectionAggregate>();

        majorityElection.CreateFrom(data);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task Update(Domain.MajorityElection data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(data.DomainOfInfluenceId, false);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            PoliticalBusinessType.MajorityElection,
            data.ReportDomainOfInfluenceLevel);
        var contestState = await _contestValidationService.EnsureNotLocked(data.ContestId);

        var existingMajorityElection = await _majorityElectionRepo.Query()
                .Include(x => x.DomainOfInfluence)
                .FirstOrDefaultAsync(x => x.Id == data.Id)
            ?? throw new EntityNotFoundException(nameof(Domain.MajorityElection), data.Id);

        if (existingMajorityElection.ContestId != data.ContestId)
        {
            throw new ValidationException($"{nameof(existingMajorityElection.ContestId)} is immutable.");
        }

        if (data.IndividualCandidatesDisabled)
        {
            await EnsureNoPrimaryElectionIndividualCandidates(data.Id);
        }

        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.Id);
        if (contestState.TestingPhaseEnded())
        {
            majorityElection.UpdateAfterTestingPhaseEnded(data);
        }
        else
        {
            majorityElection.UpdateFrom(data);
        }

        await _aggregateRepository.Save(majorityElection);
    }

    public async Task UpdateActiveState(Guid majorityElectionId, bool active)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(majorityElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.UpdateActiveState(active);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task Delete(Guid majorityElectionId)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(majorityElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.Delete();
        await _aggregateRepository.Save(majorityElection);
    }

    // Note: Candidates may be created at any time until the contest is archived
    public async Task CreateCandidate(Domain.MajorityElectionCandidate data)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.MajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        var doi = await _domainOfInfluenceRepo.GetByKey(majorityElection.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(majorityElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi, contestState.TestingPhaseEnded());

        majorityElection.CreateCandidateFrom(data, candidateValidationParams);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task UpdateCandidate(Domain.MajorityElectionCandidate data)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.MajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        var doi = await _domainOfInfluenceRepo.GetByKey(majorityElection.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(majorityElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi, contestState.TestingPhaseEnded());

        if (contestState.TestingPhaseEnded())
        {
            majorityElection.UpdateCandidateAfterTestingPhaseEnded(data, candidateValidationParams);
        }
        else
        {
            majorityElection.UpdateCandidateFrom(data, candidateValidationParams);
        }

        await _aggregateRepository.Save(majorityElection);
    }

    public async Task ReorderCandidates(Guid electionId, IReadOnlyCollection<EntityOrder> orders)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(electionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.ReorderCandidates(orders);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task DeleteCandidate(Guid candidateId)
    {
        var candidate = await _majorityElectionCandidateRepo.GetByKey(candidateId)
            ?? throw new ValidationException($"Majority election candidate does not exist: {candidateId}");

        var electionId = candidate.MajorityElectionId;
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(electionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.DeleteCandidate(candidateId);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task CreateSecondaryMajorityElection(Domain.SecondaryMajorityElection data)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.PrimaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _politicalBusinessValidationService.EnsureUniquePoliticalBusinessNumber(
            data.Id,
            majorityElection.ContestId,
            majorityElection.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            PoliticalBusinessType.SecondaryMajorityElection);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        if (majorityElection.SecondaryMajorityElections.Count == 0)
        {
            await CreateElectionGroup(majorityElection);
        }

        data.IsOnSeparateBallot = await AreSecondaryElectionsOnSeparateBallots(majorityElection.DomainOfInfluenceId);
        majorityElection.CreateSecondaryMajorityElectionFrom(data);

        await _aggregateRepository.Save(majorityElection);
    }

    public async Task UpdateSecondaryMajorityElection(Domain.SecondaryMajorityElection data)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.PrimaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _politicalBusinessValidationService.EnsureUniquePoliticalBusinessNumber(
            data.Id,
            majorityElection.ContestId,
            majorityElection.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            PoliticalBusinessType.SecondaryMajorityElection);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        if (data.IndividualCandidatesDisabled)
        {
            await EnsureNoSecondaryElectionIndividualCandidates(data.Id);
        }

        if (contestState.TestingPhaseEnded())
        {
            majorityElection.UpdateSecondaryMajorityElectionAfterTestingPhaseEnded(data);
        }
        else
        {
            majorityElection.UpdateSecondaryMajorityElectionFrom(data);
        }

        await _aggregateRepository.Save(majorityElection);
    }

    public async Task DeleteSecondaryMajorityElection(Guid id)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(id);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        DeleteSecondaryMajorityElectionWithoutChecks(majorityElection, id);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task UpdateSecondaryMajorityElectionActiveState(Guid id, bool active)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(id);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.UpdateSecondaryMajorityElectionActiveState(id, active);
        await _aggregateRepository.Save(majorityElection);
    }

    // Note: Candidates may be created at any time until the contest is archived
    public async Task CreateSecondaryMajorityElectionCandidate(Domain.MajorityElectionCandidate data)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(data.MajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);
        var doi = await _domainOfInfluenceRepo.GetByKey(majorityElection.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(majorityElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi, contestState.TestingPhaseEnded());

        majorityElection.CreateSecondaryMajorityElectionCandidateFrom(data, candidateValidationParams);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task UpdateSecondaryMajorityElectionCandidate(Domain.MajorityElectionCandidate data)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(data.MajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);
        var doi = await _domainOfInfluenceRepo.GetByKey(majorityElection.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(majorityElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi, contestState.TestingPhaseEnded());

        if (contestState.TestingPhaseEnded())
        {
            majorityElection.UpdateSecondaryMajorityElectionCandidateAfterTestingPhaseEnded(data, candidateValidationParams);
        }
        else
        {
            majorityElection.UpdateSecondaryMajorityElectionCandidateFrom(data, candidateValidationParams);
        }

        await _aggregateRepository.Save(majorityElection);
    }

    public async Task DeleteSecondaryMajorityElectionCandidate(Guid candidateId)
    {
        var secondaryMajorityElection = await _secondaryMajorityElectionRepo.Query()
            .Where(sme => sme.Candidates.Any(c => c.Id == candidateId))
            .FirstOrDefaultAsync()
            ?? throw new ValidationException($"Secondary majority election candidate does not exist: {candidateId}");

        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(secondaryMajorityElection.PrimaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.DeleteSecondaryMajorityElectionCandidate(secondaryMajorityElection.Id, candidateId);
        await _aggregateRepository.Save(majorityElection);
    }

    // Note: Candidates may be created at any time until the contest is archived
    public async Task CreateMajorityElectionCandidateReference(MajorityElectionCandidateReference data)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(data.SecondaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        majorityElection.CreateCandidateReferenceFrom(data);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task UpdateMajorityElectionCandidateReference(MajorityElectionCandidateReference data)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(data.SecondaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.UpdateCandidateReferenceFrom(data);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task DeleteMajorityElectionCandidateReference(Guid candidateId)
    {
        var secondaryMajorityElection = await _secondaryMajorityElectionRepo.Query()
            .Where(sme => sme.Candidates.Any(c => c.Id == candidateId))
            .FirstOrDefaultAsync()
            ?? throw new ValidationException($"Secondary majority election candidate reference does not exist: {candidateId}");

        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(secondaryMajorityElection.PrimaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.DeleteCandidateReference(secondaryMajorityElection.Id, candidateId);
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task ReorderSecondaryMajorityElectionCandidates(Guid secondaryMajorityElectionId, IReadOnlyCollection<EntityOrder> orders)
    {
        var majorityElection = await GetAggregateFromSecondaryMajorityElection(secondaryMajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.ReorderSecondaryMajorityElectionCandidates(secondaryMajorityElectionId, orders);
        await _aggregateRepository.Save(majorityElection);
    }

    // Note: Ballot groups may be created as long as no election of the group is active.
    public async Task CreateBallotGroup(Domain.MajorityElectionBallotGroup data)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.MajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        majorityElection.CreateBallotGroupFrom(data);
        await _aggregateRepository.Save(majorityElection);
    }

    // Note: Ballot groups may be created as long as no election of the group is active or the candidate count is ok.
    public async Task UpdateBallotGroup(Domain.MajorityElectionBallotGroup data)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(data.MajorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        majorityElection.UpdateBallotGroup(data, contestState.TestingPhaseEnded());
        await _aggregateRepository.Save(majorityElection);
    }

    public async Task DeleteBallotGroup(Guid id)
    {
        var ballotGroup = await _ballotGroupRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        var electionId = ballotGroup.MajorityElectionId;
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(electionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(majorityElection.ContestId);

        majorityElection.DeleteBallotGroup(id);
        await _aggregateRepository.Save(majorityElection);
    }

    // Note: Ballot group candidates may be updated at any time if the candidate count is not ok
    public async Task UpdateBallotGroupCandidates(MajorityElectionBallotGroupCandidates data)
    {
        var ballotGroup = await _ballotGroupRepo.Query()
            .Include(x => x.Entries)
            .ThenInclude(x => x.PrimaryMajorityElection)
            .Include(x => x.Entries)
            .ThenInclude(x => x.SecondaryMajorityElection)
            .FirstOrDefaultAsync(x => x.Id == data.BallotGroupId)
            ?? throw new ValidationException($"Ballot group does not exist: {data.BallotGroupId}");

        EnsureValidIndividualCandidatesVoteCount(ballotGroup, data);

        var electionId = ballotGroup.MajorityElectionId;
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(electionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(majorityElection.ContestId);

        majorityElection.UpdateBallotGroupCandidates(data, contestState.TestingPhaseEnded());
        await _aggregateRepository.Save(majorityElection);
    }

    internal override async Task DeleteWithoutChecks(List<Guid> majorityElectionIds)
    {
        foreach (var id in majorityElectionIds)
        {
            var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(id);

            var secondaryMajorityElectionIds = majorityElection.SecondaryMajorityElections.Select(x => x.Id).ToArray();
            foreach (var smeId in secondaryMajorityElectionIds)
            {
                DeleteSecondaryMajorityElectionWithoutChecks(majorityElection, smeId);
            }

            majorityElection.Delete();
            await _aggregateRepository.Save(majorityElection);
        }
    }

    private void DeleteSecondaryMajorityElectionWithoutChecks(MajorityElectionAggregate aggregate, Guid id)
    {
        aggregate.DeleteSecondaryMajorityElection(id);

        if (aggregate.SecondaryMajorityElections.Count == 0)
        {
            aggregate.DeleteElectionGroup();
        }
    }

    private async Task CreateElectionGroup(MajorityElectionAggregate aggregate)
    {
        var highestElectionGroupNumber = await _electionGroupRepo.Query()
            .Where(eg => eg.PrimaryMajorityElection.ContestId == aggregate.ContestId)
            .MaxAsync(eg => (int?)eg.Number) ?? 0;

        var electionGroupNumber = highestElectionGroupNumber + 1;
        var electionGroup = new Domain.ElectionGroup
        {
            PrimaryMajorityElectionId = aggregate.Id,
            Number = electionGroupNumber,
            Description = $"{DefaultElectionGroupDescriptionPrefix} {electionGroupNumber}",
        };

        aggregate.CreateElectionGroupFrom(electionGroup);
    }

    private async Task<MajorityElectionAggregate> GetAggregateFromSecondaryMajorityElection(Guid id)
    {
        var secondaryMajorityElection = await _secondaryMajorityElectionRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(nameof(Domain.SecondaryMajorityElection), id);

        return await _aggregateRepository.GetById<MajorityElectionAggregate>(secondaryMajorityElection.PrimaryMajorityElectionId);
    }

    private void EnsureValidIndividualCandidatesVoteCount(MajorityElectionBallotGroup ballotGroup, MajorityElectionBallotGroupCandidates data)
    {
        foreach (var entry in data.EntryCandidates)
        {
            var ballotGroupEntry = ballotGroup.Entries.FirstOrDefault(e => e.Id == entry.BallotGroupEntryId)
                ?? throw new ValidationException($"Ballot group entry with id {entry.BallotGroupEntryId} not found");

            var individualCandidatesDisabled = ballotGroupEntry.PrimaryMajorityElection?.IndividualCandidatesDisabled ?? ballotGroupEntry.SecondaryMajorityElection!.IndividualCandidatesDisabled;

            if (individualCandidatesDisabled && entry.IndividualCandidatesVoteCount > 0)
            {
                throw new ValidationException($"Individual candidates vote count not enabled on ballot group entry {entry.BallotGroupEntryId}");
            }
        }
    }

    private async Task EnsureNoPrimaryElectionIndividualCandidates(Guid id)
    {
        var election = await _majorityElectionRepo.Query()
            .Include(me => me.BallotGroupEntries)
            .FirstOrDefaultAsync(me => me.Id == id)
            ?? throw new EntityNotFoundException(id);

        if (election.BallotGroupEntries.Any(e => e.IndividualCandidatesVoteCount > 0))
        {
            throw new ValidationException("Cannot disable individual candidates when there are individual candidates vote count defined on ballot group entries");
        }
    }

    private async Task EnsureNoSecondaryElectionIndividualCandidates(Guid id)
    {
        var election = await _secondaryMajorityElectionRepo.Query()
            .Include(me => me.BallotGroupEntries)
            .FirstOrDefaultAsync(me => me.Id == id)
            ?? throw new EntityNotFoundException(id);

        if (election.BallotGroupEntries.Any(e => e.IndividualCandidatesVoteCount > 0))
        {
            throw new ValidationException("Cannot disable individual candidates when there are individual candidates vote count defined on ballot group entries");
        }
    }

    private async Task<bool> AreSecondaryElectionsOnSeparateBallots(Guid doiId)
    {
        return await _domainOfInfluenceRepo.Query()
            .Where(x => x.Id == doiId && x.CantonDefaults.SecondaryMajorityElectionOnSeparateBallot)
            .AnyAsync();
    }
}
