// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

public class VoteWriter
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
        IDbRepository<DataContext, DomainOfInfluence> doiRepository)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _politicalBusinessValidationService = politicalBusinessValidationService;
        _contestValidationService = contestValidationService;
        _voteRepository = voteRepository;
        _doiRepository = doiRepository;
    }

    public async Task Create(Domain.Vote data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(data.DomainOfInfluenceId);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
            data.ReportDomainOfInfluenceLevel);
        await _contestValidationService.EnsureInTestingPhase(data.ContestId);

        var vote = _aggregateFactory.New<VoteAggregate>();

        vote.CreateFrom(data);
        await _aggregateRepository.Save(vote);
    }

    public async Task Update(Domain.Vote data)
    {
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(data.DomainOfInfluenceId);
        await _politicalBusinessValidationService.EnsureValidEditData(
            data.Id,
            data.ContestId,
            data.DomainOfInfluenceId,
            data.PoliticalBusinessNumber,
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

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(vote.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        vote.UpdateActiveState(active);
        await _aggregateRepository.Save(vote);
    }

    public async Task Delete(Guid voteId)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(voteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(vote.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        vote.Delete();
        await _aggregateRepository.Save(vote);
    }

    public async Task CreateBallot(Ballot data)
    {
        var vote = await _aggregateRepository.GetById<VoteAggregate>(data.VoteId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(vote.DomainOfInfluenceId);
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

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(vote.DomainOfInfluenceId);
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

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(vote.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(vote.ContestId);

        vote.DeleteBallot(id);
        await _aggregateRepository.Save(vote);
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
