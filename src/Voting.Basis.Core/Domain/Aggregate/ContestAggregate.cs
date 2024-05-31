// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="Contest"/>.
/// </summary>
public sealed class ContestAggregate : BaseDeletableAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<Contest> _validator;
    private readonly IClock _clock;
    private readonly PublisherConfig _config;

    public ContestAggregate(IMapper mapper, EventInfoProvider eventInfoProvider, IValidator<Contest> validator, IClock clock, PublisherConfig config)
    {
        Description = new Dictionary<string, string>();
        MergedContestIds = new List<Guid>();
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _clock = clock;
        _config = config;
    }

    public override string AggregateName => "voting-contests";

    public DateTime Date { get; private set; }

    public Dictionary<string, string> Description { get; private set; }

    public DateTime EndOfTestingPhase { get; private set; }

    public DateTime? ArchivePer { get; private set; }

    public DateTime PastLockPer { get; private set; }

    public Guid DomainOfInfluenceId { get; private set; }

    public bool EVoting { get; private set; }

    public DateTime? EVotingFrom { get; private set; }

    public DateTime? EVotingTo { get; private set; }

    public ContestState State { get; private set; } = ContestState.TestingPhase;

    public List<Guid> MergedContestIds { get; }

    public Guid? PreviousContestId { get; private set; }

    public void CreateFrom(Contest contest)
    {
        if (contest.Id == default)
        {
            contest.Id = Guid.NewGuid();
        }

        _validator.ValidateAndThrow(contest);
        NormalizeAndValidateDates(contest);

        contest.State = State;
        var ev = new ContestCreated
        {
            Contest = _mapper.Map<ContestEventData>(contest),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(contest.Id));
    }

    public void UpdateFrom(Contest contest)
    {
        EnsureNotDeleted();
        EnsureModificationsAllowed();
        _validator.ValidateAndThrow(contest);
        NormalizeAndValidateDates(contest);

        if (contest.DomainOfInfluenceId != DomainOfInfluenceId)
        {
            throw new ValidationException($"{nameof(DomainOfInfluenceId)} is immutable.");
        }

        contest.State = State;
        var ev = new ContestUpdated
        {
            Contest = _mapper.Map<ContestEventData>(contest),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete()
    {
        EnsureNotDeleted();
        EnsureModificationsAllowed();
        var ev = new ContestDeleted
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void MergeContests(IEnumerable<Guid> contestIds)
    {
        EnsureNotDeleted();
        EnsureModificationsAllowed();
        var ids = contestIds.ToList();

        if (MergedContestIds.Any(mid => ids.Contains(mid)))
        {
            throw new ValidationException("A contest was already merged into this aggregate");
        }

        var ev = new ContestsMerged
        {
            MergedId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };
        ev.OldIds.AddRange(ids.Select(x => x.ToString()));

        RaiseEvent(ev);
    }

    public bool TryEndTestingPhase()
    {
        EnsureNotDeleted();

        if (State != ContestState.TestingPhase)
        {
            return false;
        }

        if (_clock.UtcNow < EndOfTestingPhase)
        {
            throw new ValidationException("End of testing phase not yet reached");
        }

        var ev = new ContestTestingPhaseEnded
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        return true;
    }

    public bool TrySetPastLocked()
    {
        EnsureNotDeleted();

        if (State.IsLocked())
        {
            return false;
        }

        if (_clock.UtcNow < PastLockPer)
        {
            throw new ValidationException("Contest past lock per not yet reached");
        }

        var ev = new ContestPastLocked
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        return true;
    }

    public bool TryArchive()
    {
        if (State != ContestState.PastLocked)
        {
            return false;
        }

        var ev = new ContestArchived
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        return true;
    }

    /// <summary>
    /// Archives the contest.
    /// </summary>
    /// <param name="archivePer">
    /// If null, the contest is archived immediately.
    /// Otherwise the archival is scheduled for the specified date.
    /// </param>
    /// <exception cref="ValidationException">
    /// If the provided date is not in the future or not after the contest date.
    /// If no archivePer is provided and the contest is not in the past yet.
    /// </exception>
    public void Archive(DateTime? archivePer = null)
    {
        if (State != ContestState.PastLocked)
        {
            throw new ValidationException("a contest can only be archived if it is in the past state");
        }

        if (!archivePer.HasValue)
        {
            var archivedEvent = new ContestArchived
            {
                ContestId = Id.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            };

            RaiseEvent(archivedEvent);
            return;
        }

        if (archivePer < Date || archivePer < _clock.UtcNow)
        {
            throw new ValidationException("archive per has to be after the contest date and in the future");
        }

        var ev = new ContestArchiveDateUpdated
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ArchivePer = archivePer.Value.ToTimestamp(),
        };
        RaiseEvent(ev);
    }

    public void PastUnlock()
    {
        if (State != ContestState.PastLocked)
        {
            throw new ValidationException("a contest can only be unlocked if it is in the past locked state");
        }

        var ev = new ContestPastUnlocked
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };
        RaiseEvent(ev);
    }

    public void StartContestImport()
    {
        var ev = new ContestImportStarted
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void StartPoliticalBusinessesImport()
    {
        var ev = new PoliticalBusinessesImportStarted
        {
            ContestId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ContestCreated e:
                Apply(e);
                State = ContestState.TestingPhase;
                break;
            case ContestUpdated e:
                Apply(e);
                break;
            case ContestsMerged e:
                Apply(e);
                break;
            case ContestDeleted _:
                Deleted = true;
                break;
            case ContestArchiveDateUpdated e:
                ArchivePer = e.ArchivePer.ToDateTime();
                break;
            case ContestArchived e:
                Apply(e);
                break;
            case ContestTestingPhaseEnded _:
                State = ContestState.Active;
                break;
            case ContestPastLocked _:
                State = ContestState.PastLocked;
                break;
            case ContestPastUnlocked e:
                Apply(e);
                break;
            case ContestImportStarted _:
            case PoliticalBusinessesImportStarted _:
                break;
        }
    }

    private void RaiseEvent(IMessage eventData)
    {
        RaiseEvent(eventData, EventSignatureBusinessMetadataBuilder.BuildFrom(Id));
    }

    private void Apply(ContestArchived e)
    {
        // the date of the event can be before the archive per date
        // if an archive date is set in the future but the user selects archive now.
        var eventDate = e.EventInfo.Timestamp.ToDateTime();
        if (ArchivePer == null || ArchivePer > eventDate)
        {
            ArchivePer = eventDate;
        }

        State = ContestState.Archived;
    }

    private void Apply(ContestCreated ev)
    {
        _mapper.Map(ev.Contest, this);
    }

    private void Apply(ContestUpdated ev)
    {
        _mapper.Map(ev.Contest, this);
    }

    private void Apply(ContestsMerged ev)
    {
        MergedContestIds.AddRange(ev.OldIds.Select(GuidParser.Parse));
    }

    private void Apply(ContestPastUnlocked ev)
    {
        PastLockPer = ev.EventInfo.Timestamp.ToDateTime().NextUtcDate(true);
        State = ContestState.PastUnlocked;
    }

    private void EnsureModificationsAllowed()
    {
        if (State.TestingPhaseEnded() || _clock.UtcNow > EndOfTestingPhase)
        {
            throw new ContestTestingPhaseEndedException();
        }
    }

    private void EnsureNotLocked()
    {
        if (State.IsLocked())
        {
            throw new ContestLockedException();
        }
    }

    private void NormalizeAndValidateDates(Contest contest)
    {
        contest.Date = contest.Date.Date;

        if (contest.EndOfTestingPhase >= contest.Date)
        {
            throw new ValidationException("The testing phase must be smaller than the contest date");
        }

        if (contest.EndOfTestingPhase < _clock.UtcNow)
        {
            throw new ValidationException("The end of the testing phase must be in the future");
        }

        if (contest.EndOfTestingPhase.Add(_config.Contest.EndOfTestingPhaseMaxTimespanBeforeContest) < contest.Date)
        {
            throw new ValidationException($"The testing phase must end at the earliest {_config.Contest.EndOfTestingPhaseMaxTimespanBeforeContest} before the contest date");
        }

        if (contest.EVoting)
        {
            if (contest.EVotingFrom >= contest.EVotingTo)
            {
                throw new ValidationException("E-Voting from must be greater than E-Voting to");
            }

            if (contest.EVotingTo > contest.Date.NextUtcDate(true))
            {
                throw new ValidationException("E-Voting cannot take place after the contest");
            }
        }
    }
}
