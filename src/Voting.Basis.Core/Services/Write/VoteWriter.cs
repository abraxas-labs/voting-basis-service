// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Ballot = Voting.Basis.Core.Domain.Ballot;

namespace Voting.Basis.Core.Services.Write;

public class VoteWriter : PoliticalBusinessWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly PoliticalBusinessValidationService _politicalBusinessValidationService;
    private readonly ContestValidationService _contestValidationService;
    private readonly IDbRepository<DataContext, Vote> _voteRepository;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepository;

    public VoteWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        PoliticalBusinessValidationService politicalBusinessValidationService,
        ContestValidationService contestValidationService,
        IDbRepository<DataContext, Vote> voteRepository,
        IDbRepository<DataContext, DomainOfInfluence> doiRepository,
        IDbRepository<DataContext, Contest> contestRepo)
        : base(doiRepository, contestRepo)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _politicalBusinessValidationService = politicalBusinessValidationService;
        _contestValidationService = contestValidationService;
        _voteRepository = voteRepository;
        _doiRepository = doiRepository;
    }

    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public async Task Create(Domain.Vote data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(data.DomainOfInfluenceId, false);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            PoliticalBusinessType.Vote,
            data.ReportDomainOfInfluenceLevel);
        await _contestValidationService.EnsureInTestingPhase(data.ContestId);
        await HandleEVotingDuringCreation(data);

        var vote = _aggregateFactory.New<VoteAggregate>();

        vote.CreateFrom(data);
        await _aggregateRepository.Save(vote);
    }

    public async Task Update(Domain.Vote data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(data.DomainOfInfluenceId, false);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            PoliticalBusinessType.Vote,
            data.ReportDomainOfInfluenceLevel);
        var contestState = await _contestValidationService.EnsureNotLocked(data.ContestId);

        var voteModel = await _voteRepository.GetByKey(data.Id)
            ?? throw new EntityNotFoundException(nameof(Domain.Vote), data.Id);

        if (voteModel.ContestId != data.ContestId)
        {
            throw new ValidationException($"{nameof(voteModel.ContestId)} is immutable.");
        }

        var vote = await _aggregateRepository.GetById<VoteAggregate>(data.Id);
        if (contestState.TestingPhaseEnded())
        {
            vote.UpdateAfterTestingPhaseEnded(data);
        }
        else
        {
            vote.UpdateFrom(data);
        }

        await _aggregateRepository.Save(vote);
    }

    public async Task UpdateActiveState(Guid voteId, bool active)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(voteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        vote.UpdateActiveState(active);
        await _aggregateRepository.Save(vote);
    }

    public async Task UpdateEVotingApproval(Guid voteId, bool approved)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(voteId);

        _permissionService.EnsureCanSetEVotingApproval(PoliticalBusinessType.Vote, approved);

        // E-Voting approval is a special permission, which can be applied even if the user has only read political business readpermissions.
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, true);
        await _contestValidationService.EnsureNotLocked(vote.ContestId);
        await _contestValidationService.EnsureCanChangePoliticalBusinessEVotingApproval(vote.ContestId, approved);

        if (approved && !vote.Active)
        {
            vote.UpdateActiveState(true);
        }

        vote.UpdateEVotingApproval(approved);
        await _aggregateRepository.Save(vote);
    }

    public async Task Delete(Guid voteId)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(voteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        vote.Delete();
        await _aggregateRepository.Save(vote);
    }

    public async Task CreateBallot(Ballot data)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(data.VoteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        if (data.Position != 1)
        {
            await EnsureMultipleVoteBallotsEnabled(vote.DomainOfInfluenceId);
            EnsureIsContinuousBallotPosition(data.Position, vote.Ballots.Count);
        }

        vote.CreateBallot(data);
        await _aggregateRepository.Save(vote);
    }

    public async Task UpdateBallot(Ballot data)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(data.VoteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, false);
        var contestState = await _contestValidationService.EnsureNotLocked(vote.ContestId);

        if (contestState.TestingPhaseEnded())
        {
            vote.UpdateBallotAfterTestingPhaseEnded(data);
        }
        else
        {
            vote.UpdateBallot(data);
        }

        await _aggregateRepository.Save(vote);
    }

    public async Task DeleteBallot(Guid id, Guid voteId)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(voteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, false);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        vote.DeleteBallot(id);
        await _aggregateRepository.Save(vote);
    }

    internal async Task<bool> TryApproveEVoting(Guid voteId)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(voteId);

        if (!vote.TryApproveEVoting())
        {
            return false;
        }

        await _aggregateRepository.Save(vote);
        return true;
    }

    internal override async Task DeleteWithoutChecks(List<Guid> ids)
    {
        foreach (var id in ids)
        {
            var aggregate = await _aggregateRepository.GetById<VoteAggregate>(id);
            aggregate.Delete(true);
            await _aggregateRepository.Save(aggregate);
        }
    }

    private async Task EnsureMultipleVoteBallotsEnabled(Guid doiId)
    {
        var doi = await _doiRepository.GetByKey(doiId)
            ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), doiId);

        if (!doi.CantonDefaults.MultipleVoteBallotsEnabled)
        {
            throw new ValidationException("Multiple vote ballots are not enabled for this canton.");
        }
    }

    private void EnsureIsContinuousBallotPosition(int ballotPosition, int ballotsCount)
    {
        if (ballotPosition == ballotsCount + 1)
        {
            return;
        }

        throw new ValidationException($"The ballot position {ballotPosition} is invalid, is non-continuous.");
    }
}
