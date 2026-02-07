// (c) Copyright by Abraxas Informatik AG
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

    public bool AutomaticBallotNumberGeneration { get; private set; }

    public bool AutomaticBallotBundleNumberGeneration { get; private set; }

    public bool EnforceReviewProcedureForCountingCircles { get; private set; }

    public VoteType Type { get; private set; }

    public bool? EVotingApproved { get; private set; }

    private bool TypeImmutable { get; set; }

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
        EnsureEVotingNotApproved();

        // We only set a different e-voting approved on create or approval update.
        vote.EVotingApproved = EVotingApproved;

        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, vote.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(ResultAlgorithm, vote.ResultAlgorithm);

        if (TypeImmutable)
        {
            ValidationUtils.EnsureNotModified(Type, vote.Type);
        }

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
        EnsureEVotingNotApproved();

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        vote.Active = Active;

        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, vote.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(ContestId, vote.ContestId);
        ValidationUtils.EnsureNotModified(ResultAlgorithm, vote.ResultAlgorithm);
        ValidationUtils.EnsureNotModified(ReviewProcedure, vote.ReviewProcedure);
        ValidationUtils.EnsureNotModified(EnforceReviewProcedureForCountingCircles, vote.EnforceReviewProcedureForCountingCircles);
        ValidationUtils.EnsureNotModified(AutomaticBallotNumberGeneration, vote.AutomaticBallotNumberGeneration);
        ValidationUtils.EnsureNotModified(AutomaticBallotNumberGeneration, vote.AutomaticBallotBundleNumberGeneration);
        ValidationUtils.EnsureNotModified(Type, vote.Type);

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
        EnsureCanSetActive();
        EnsureEVotingNotApproved();

        var ev = new VoteActiveStateUpdated
        {
            VoteId = Id.ToString(),
            Active = active,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateEVotingApproval(bool approved)
    {
        EnsureCanSetActive();

        if (EVotingApproved == null)
        {
            throw new ValidationException($"Vote {Id} does not support E-Voting");
        }

        var ev = new VoteEVotingApprovalUpdated
        {
            VoteId = Id.ToString(),
            Approved = approved,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public bool TryApproveEVoting()
    {
        if (EVotingApproved == true)
        {
            return false;
        }

        try
        {
            UpdateEVotingApproval(true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void CreateBallot(Ballot ballot)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();

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
        EnsureEVotingNotApproved();

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
        EnsureEVotingNotApproved();

        var existingBallot = Ballots.Find(b => b.Id == ballot.Id)
                             ?? throw new EntityNotFoundException("ballot not found");

        ballot.Position = existingBallot.Position;
        _ballotValidator.ValidateAndThrow(ballot);

        ValidationUtils.EnsureNotModified(existingBallot.BallotType, ballot.BallotType);
        ValidationUtils.EnsureNotModified(existingBallot.SubType, ballot.SubType);
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
            BallotQuestions = { ballot.BallotQuestions.Select(_mapper.Map<BallotQuestionEventData>) },
            TieBreakQuestions = { ballot.TieBreakQuestions.Select(_mapper.Map<TieBreakQuestionEventData>) },
        };

        RaiseEvent(ev);
        EnsureValidResultEntry();
    }

    public void Delete(bool ignoreCheck = false)
    {
        EnsureNotDeleted();

        if (!ignoreCheck)
        {
            EnsureEVotingNotApproved();
        }

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
        EnsureEVotingNotApproved();

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
            case VoteEVotingApprovalUpdated e:
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
        PatchOldEvents(ev.Vote);
        _mapper.Map(ev.Vote, this);
    }

    private void Apply(VoteUpdated ev)
    {
        PatchOldEvents(ev.Vote);
        _mapper.Map(ev.Vote, this);
    }

    private void PatchOldEvents(VoteEventData vote)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (vote.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure.Unspecified)
        {
            vote.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure.Electronically;
        }

        if (vote.Type == Abraxas.Voting.Basis.Shared.V1.VoteType.Unspecified)
        {
            vote.Type = Abraxas.Voting.Basis.Shared.V1.VoteType.QuestionsOnSingleBallot;
        }

        if (vote.AutomaticBallotNumberGeneration == null)
        {
            vote.AutomaticBallotNumberGeneration = true;
        }
    }

    private void Apply(VoteAfterTestingPhaseUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(VoteActiveStateUpdated ev)
    {
        Active = ev.Active;
    }

    private void Apply(VoteEVotingApprovalUpdated ev)
    {
        EVotingApproved = ev.Approved;
    }

    private void Apply(BallotCreated ev)
    {
        TypeImmutable = true;
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
        var resultEntryDetailedAllowed = Ballots.Count == 1
            && Ballots[0].BallotType == BallotType.VariantsBallot
            && Type != VoteType.VariantQuestionsOnMultipleBallots;

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

    private void EnsureValidBallots()
    {
        if (Ballots.Count == 0)
        {
            throw new PoliticalBusinessNotCompleteException("The vote does not have a ballot");
        }

        if (Type == VoteType.QuestionsOnSingleBallot)
        {
            if (Ballots.Count > 1)
            {
                throw new ValidationException("A vote with all questions on a single ballot cannot have multiple ballots");
            }

            if (Ballots.Any(x => x.SubType != BallotSubType.Unspecified))
            {
                throw new ValidationException("A vote with all questions on a single ballot cannot have a ballot sub type");
            }
        }

        if (Type == VoteType.VariantQuestionsOnMultipleBallots)
        {
            var lastSubType = BallotSubType.Unspecified;
            foreach (var ballot in Ballots.OrderBy(x => x.Position))
            {
                var subType = ballot.SubType;
                if (subType == BallotSubType.Unspecified)
                {
                    throw new ValidationException("A vote with all questions on separate ballots must have ballot sub types");
                }

                if (subType <= lastSubType)
                {
                    throw new ValidationException("The order of ballot sub types isn't correct");
                }

                lastSubType = subType;
            }
        }
    }

    private void EnsureCanSetActive()
    {
        EnsureNotDeleted();
        EnsureValidBallots();
    }

    private void EnsureEVotingNotApproved()
    {
        if (EVotingApproved == true)
        {
            throw new PoliticalBusinessEVotingApprovedException();
        }
    }
}
