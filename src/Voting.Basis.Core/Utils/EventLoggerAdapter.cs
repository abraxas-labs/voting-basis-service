// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Utils;

public class EventLoggerAdapter
{
    private readonly EventLogger _eventLogger;

    public EventLoggerAdapter(EventLogger eventLogger)
    {
        _eventLogger = eventLogger;
    }

    public async Task LogCountingCircleEvent<T>(T eventData, CountingCircle countingCircle)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            CountingCircleId = countingCircle.Id,
        });
    }

    public async Task LogDomainOfInfluenceEvent<T>(T eventData, Guid domainOfInfluenceId)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            DomainOfInfluenceId = domainOfInfluenceId,
        });
    }

    public async Task LogDomainOfInfluenceEvent<T>(T eventData, DomainOfInfluence domainOfInfluence)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            DomainOfInfluenceId = domainOfInfluence.Id,
        });
    }

    public async Task LogContestEvent<T>(T eventData, Contest contest)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            ContestId = contest.Id,
            DomainOfInfluenceId = contest.DomainOfInfluenceId,
        });
    }

    public async Task LogVoteEvent<T>(T eventData, Vote vote)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = vote.ContestId,
            PoliticalBusinessId = vote.Id,
            DomainOfInfluenceId = vote.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogBallotEvent<T>(T eventData, Ballot ballot, Guid? contestId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = contestId ?? ballot.Vote.ContestId,
            PoliticalBusinessId = ballot.VoteId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionEvent<T>(T eventData, ProportionalElection election)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = election.ContestId,
            PoliticalBusinessId = election.Id,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionListEvent<T>(T eventData, ProportionalElectionList list, Guid? contestId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = contestId ?? list.ProportionalElection.ContestId,
            PoliticalBusinessId = list.ProportionalElectionId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionListUnionEvent<T>(T eventData, ProportionalElectionListUnion listUnion, Guid? contestId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = contestId ?? listUnion.ProportionalElection.ContestId,
            PoliticalBusinessId = listUnion.ProportionalElectionId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionCandidateEvent<T>(T eventData, ProportionalElectionCandidate candidate, ProportionalElectionList? list = null)
        where T : IMessage<T>
    {
        var proportionalElectionList = list ?? candidate.ProportionalElectionList;

        var eventLog = new EventLog
        {
            ContestId = proportionalElectionList.ProportionalElection.ContestId,
            PoliticalBusinessId = proportionalElectionList.ProportionalElectionId,
            DomainOfInfluenceId = proportionalElectionList.ProportionalElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionUnionEvent<T>(T eventData, ProportionalElectionUnion union)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = union.ContestId,
            PoliticalBusinessUnionId = union.Id,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogElectionGroupEvent<T>(T eventData, ElectionGroup electionGroup)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = electionGroup.PrimaryMajorityElection.ContestId,
            PoliticalBusinessId = electionGroup.PrimaryMajorityElectionId,
            DomainOfInfluenceId = electionGroup.PrimaryMajorityElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogMajorityElectionBallotGroupEvent<T>(T eventData, MajorityElectionBallotGroup ballotGroup)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = ballotGroup.MajorityElection.ContestId,
            PoliticalBusinessId = ballotGroup.MajorityElectionId,
            DomainOfInfluenceId = ballotGroup.MajorityElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogMajorityElectionEvent<T>(T eventData, MajorityElection election)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = election.ContestId,
            PoliticalBusinessId = election.Id,
            DomainOfInfluenceId = election.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogMajorityElectionCandidateEvent<T>(T eventData, MajorityElectionCandidate candidate, Guid? contestId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = contestId ?? candidate.MajorityElection.ContestId,
            PoliticalBusinessId = candidate.MajorityElectionId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogSecondaryMajorityElectionEvent<T>(T eventData, SecondaryMajorityElection election, Guid? contestId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = contestId ?? election.ContestId,
            PoliticalBusinessId = election.Id,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogSecondaryMajorityElectionCandidateEvent<T>(T eventData, SecondaryMajorityElectionCandidate candidate, Guid? contestId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = contestId ?? candidate.SecondaryMajorityElection.ContestId,
            PoliticalBusinessId = candidate.SecondaryMajorityElectionId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogMajorityElectionUnionEvent<T>(T eventData, MajorityElectionUnion union)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            ContestId = union.ContestId,
            PoliticalBusinessUnionId = union.Id,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }
}
