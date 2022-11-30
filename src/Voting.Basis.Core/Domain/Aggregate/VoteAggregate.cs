// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="Vote"/>.
/// </summary>
public class VoteAggregate : BaseHasContestAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<Ballot> _ballotValidator;

    public VoteAggregate(
        IMapper mapper,
        EventInfoProvider eventInfoProvider,
        IValidator<Ballot> ballotValidator)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _ballotValidator = ballotValidator;

        PoliticalBusinessNumber = string.Empty;
        OfficialDescription = new Dictionary<string, string>();
        ShortDescription = new Dictionary<string, string>();

        Ballots = new List<Ballot>();
    }

    public override string AggregateName => "voting-votes";

    public string PoliticalBusinessNumber { get; private set; }

    public Dictionary<string, string> OfficialDescription { get; private set; }

    public Dictionary<string, string> ShortDescription { get; private set; }

    public Guid DomainOfInfluenceId { get; private set; }

    public bool Active { get; private set; }

    public List<Ballot> Ballots { get; private set; }

    public VoteResultAlgorithm ResultAlgorithm { get; private set; }

    public VoteResultEntry ResultEntry { get; private set; }

    public bool EnforceResultEntryForCountingCircles { get; private set; }

    public VoteReviewProcedure ReviewProcedure { get; private set; }

    public bool EnforceReviewProcedureForCountingCircles { get; private set; }

    public void CreateFrom(Vote vote)
    {
        if (vote.Id == default)
        {
            vote.Id = Guid.NewGuid();
        }

        var ev = new VoteCreated
        {
            Vote = _mapper.Map<VoteEventData>(vote),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(vote.ContestId));
        EnsureValidResultEntry();
    }

    public void UpdateFrom(Vote vote)
    {
        EnsureNotDeleted();

        var ev = new VoteUpdated
        {
            Vote = _mapper.Map<VoteEventData>(vote),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        EnsureValidResultEntry();
    }

    public void UpdateAfterTestingPhaseEnded(Vote vote)
    {
        EnsureNotDeleted();

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        vote.Active = Active;

        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, vote.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(ContestId, vote.ContestId);
        ValidationUtils.EnsureNotModified(ResultAlgorithm, vote.ResultAlgorithm);
        ValidationUtils.EnsureNotModified(ReviewProcedure, vote.ReviewProcedure);
        ValidationUtils.EnsureNotModified(EnforceReviewProcedureForCountingCircles, vote.EnforceReviewProcedureForCountingCircles);

        var ev = new VoteAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = vote.Id.ToString(),
            PoliticalBusinessNumber = vote.PoliticalBusinessNumber,
            InternalDescription = vote.InternalDescription,
            OfficialDescription = { vote.OfficialDescription },
            ShortDescription = { vote.ShortDescription },
            ReportDomainOfInfluenceLevel = vote.ReportDomainOfInfluenceLevel,
        };

        RaiseEvent(ev);
    }

    public void UpdateActiveState(bool active)
    {
        EnsureNotDeleted();
        var ev = new VoteActiveStateUpdated
        {
            VoteId = Id.ToString(),
            Active = active,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateBallot(Ballot ballot)
    {
        EnsureNotDeleted();
        if (ballot.Id == default)
        {
            ballot.Id = Guid.NewGuid();
        }

        if (Ballots.Any(b => b.Id == ballot.Id || b.Position == ballot.Position))
        {
            throw new ValidationException("Ballot already exists");
        }

        _ballotValidator.ValidateAndThrow(ballot);

        var ev = new BallotCreated
        {
            Ballot = _mapper.Map<BallotEventData>(ballot),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        EnsureValidResultEntry();
    }

    public void UpdateBallot(Ballot ballot)
    {
        EnsureNotDeleted();
        var existingBallot = Ballots.Find(b => b.Id == ballot.Id)
                             ?? throw new EntityNotFoundException("ballot not found");

        ballot.Position = existingBallot.Position;
        _ballotValidator.ValidateAndThrow(ballot);

        var ev = new BallotUpdated
        {
            Ballot = _mapper.Map<BallotEventData>(ballot),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        EnsureValidResultEntry();
    }

    public void UpdateBallotAfterTestingPhaseEnded(Ballot ballot)
    {
        EnsureNotDeleted();
        var existingBallot = Ballots.Find(b => b.Id == ballot.Id)
                             ?? throw new EntityNotFoundException("ballot not found");

        ballot.Position = existingBallot.Position;
        _ballotValidator.ValidateAndThrow(ballot);

        ValidationUtils.EnsureNotModified(existingBallot.BallotType, ballot.BallotType);
        ValidationUtils.EnsureNotModified(existingBallot.Position, ballot.Position);
        ValidationUtils.EnsureNotModified(existingBallot.BallotQuestions.Count, ballot.BallotQuestions.Count);
        ValidationUtils.EnsureNotModified(existingBallot.HasTieBreakQuestions, ballot.HasTieBreakQuestions);
        ValidationUtils.EnsureNotModified(existingBallot.TieBreakQuestions.Count, ballot.TieBreakQuestions.Count);

        var existingQuestionNumbers = existingBallot.BallotQuestions.Select(q => q.Number).ToHashSet();
        var updatedQuestionNumbers = ballot.BallotQuestions.Select(q => q.Number);
        if (!existingQuestionNumbers.SetEquals(updatedQuestionNumbers))
        {
            throw new ValidationException("Ballot doesn't contain the same questions");
        }

        var tieBreakQuestionsByNumber = ballot.TieBreakQuestions.ToDictionary(x => x.Number);
        foreach (var existingTieBreakQuestion in existingBallot.TieBreakQuestions)
        {
            var matchingQuestion = tieBreakQuestionsByNumber.GetValueOrDefault(existingTieBreakQuestion.Number)
                ?? throw new EntityNotFoundException("Tie break question not found");

            if (matchingQuestion.Question1Number != existingTieBreakQuestion.Question1Number || matchingQuestion.Question2Number != existingTieBreakQuestion.Question2Number)
            {
                throw new EntityNotFoundException("Ballot doesn't contain the same tie break questions");
            }
        }

        var ev = new BallotAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = ballot.Id.ToString(),
            VoteId = Id.ToString(),
            Description = { ballot.Description },
            BallotQuestions = { ballot.BallotQuestions.Select(_mapper.Map<BallotQuestionEventData>) },
            TieBreakQuestions = { ballot.TieBreakQuestions.Select(_mapper.Map<TieBreakQuestionEventData>) },
        };

        RaiseEvent(ev);
        EnsureValidResultEntry();
    }

    public void Delete()
    {
        EnsureNotDeleted();
        var ev = new VoteDeleted
        {
            VoteId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteBallot(Guid ballotId)
    {
        EnsureNotDeleted();

        if (Ballots.All(b => b.Id != ballotId))
        {
            throw new EntityNotFoundException(ballotId);
        }

        var ev = new BallotDeleted
        {
            BallotId = ballotId.ToString(),
            VoteId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        EnsureValidResultEntry();
    }

    public override void MoveToNewContest(Guid newContestId)
    {
        EnsureNotDeleted();
        EnsureDifferentContest(newContestId);

        var ev = new VoteToNewContestMoved
        {
            VoteId = Id.ToString(),
            NewContestId = newContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(newContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case VoteCreated e:
                Apply(e);
                break;
            case VoteUpdated e:
                Apply(e);
                break;
            case VoteAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case VoteActiveStateUpdated e:
                Apply(e);
                break;
            case VoteDeleted _:
                Deleted = true;
                break;
            case VoteToNewContestMoved e:
                ContestId = GuidParser.Parse(e.NewContestId);
                break;
            case BallotCreated e:
                Apply(e);
                break;
            case BallotUpdated e:
                Apply(e);
                break;
            case BallotAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case BallotDeleted e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(VoteCreated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.Vote.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure.Unspecified)
        {
            ev.Vote.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure.Electronically;
        }

        _mapper.Map(ev.Vote, this);
    }

    private void Apply(VoteUpdated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.Vote.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure.Unspecified)
        {
            ev.Vote.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure.Electronically;
        }

        _mapper.Map(ev.Vote, this);
    }

    private void Apply(VoteAfterTestingPhaseUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(VoteActiveStateUpdated ev)
    {
        Active = ev.Active;
    }

    private void Apply(BallotCreated ev)
    {
        Ballots.Add(_mapper.Map<Ballot>(ev.Ballot));
    }

    private void Apply(BallotUpdated ev)
    {
        var ballotId = GuidParser.Parse(ev.Ballot.Id);
        Ballots = Ballots
            .Where(b => b.Id != ballotId)
            .ToList();

        Ballots.Add(_mapper.Map<Ballot>(ev.Ballot));
    }

    private void Apply(BallotAfterTestingPhaseUpdated ev)
    {
        var id = GuidParser.Parse(ev.Id);
        var existingBallot = Ballots.Single(b => b.Id == id);

        var mappedBallot = _mapper.Map<Ballot>(ev);
        existingBallot.Description = mappedBallot.Description;
        existingBallot.BallotQuestions.Clear();
        existingBallot.BallotQuestions.AddRange(mappedBallot.BallotQuestions);
        existingBallot.TieBreakQuestions.Clear();
        existingBallot.TieBreakQuestions.AddRange(mappedBallot.TieBreakQuestions);
    }

    private void Apply(BallotDeleted ev)
    {
        var ballotId = GuidParser.Parse(ev.BallotId);
        Ballots = Ballots
            .Where(b => b.Id != ballotId)
            .ToList();
    }

    private void EnsureValidResultEntry()
    {
        var resultEntryDetailedAllowed = Ballots.Count == 1 && Ballots[0].BallotType == BallotType.VariantsBallot;
        if (resultEntryDetailedAllowed)
        {
            return;
        }

        if (ResultEntry == VoteResultEntry.Detailed)
        {
            throw new ValidationException("detailed result entry is only allowed if exactly one variants ballot exists");
        }

        if (!EnforceResultEntryForCountingCircles)
        {
            throw new ValidationException(
                "since the detailed result entry is not allowed for this vote, final result entry must be enforced");
        }
    }
}
