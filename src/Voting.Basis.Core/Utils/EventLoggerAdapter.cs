// (c) Copyright by Abraxas Informatik AG
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
            EntityId = countingCircle.Id,
            AggregateId = countingCircle.Id,
            CountingCircleId = countingCircle.Id,
        });
    }

    public async Task LogDomainOfInfluenceEvent<T>(T eventData, Guid domainOfInfluenceId)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            EntityId = domainOfInfluenceId,
            AggregateId = domainOfInfluenceId,
            DomainOfInfluenceId = domainOfInfluenceId,
        });
    }

    public async Task LogDomainOfInfluenceEvent<T>(T eventData, DomainOfInfluence domainOfInfluence)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            EntityId = domainOfInfluence.Id,
            AggregateId = domainOfInfluence.Id,
            DomainOfInfluenceId = domainOfInfluence.Id,
        });
    }

    public async Task LogContestEvent<T>(T eventData, Contest contest)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            EntityId = contest.Id,
            AggregateId = contest.Id,
            ContestId = contest.Id,
            DomainOfInfluenceId = contest.DomainOfInfluenceId,
        });
    }

    public async Task LogPoliticalAssemblyEvent<T>(T eventData, PoliticalAssembly politicalAssembly)
        where T : IMessage<T>
    {
        await _eventLogger.LogEvent(eventData, new EventLog
        {
            EntityId = politicalAssembly.Id,
            AggregateId = politicalAssembly.Id,
            PoliticalAssemblyId = politicalAssembly.Id,
            DomainOfInfluenceId = politicalAssembly.DomainOfInfluenceId,
        });
    }

    public async Task LogVoteEvent<T>(T eventData, Vote vote)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = vote.Id,
            AggregateId = vote.Id,
            ContestId = vote.ContestId,
            PoliticalBusinessId = vote.Id,
            DomainOfInfluenceId = vote.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogBallotEvent<T>(T eventData, Ballot ballot, Guid? contestId = null, Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = ballot.Id,
            AggregateId = ballot.VoteId,
            ContestId = contestId ?? ballot.Vote.ContestId,
            PoliticalBusinessId = ballot.VoteId,
            DomainOfInfluenceId = doiId ?? ballot.Vote.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionEvent<T>(T eventData, ProportionalElection election)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = election.Id,
            AggregateId = election.Id,
            ContestId = election.ContestId,
            PoliticalBusinessId = election.Id,
            DomainOfInfluenceId = election.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionListEvent<T>(T eventData, ProportionalElectionList list, Guid? contestId = null, Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = list.Id,
            AggregateId = list.ProportionalElectionId,
            ContestId = contestId ?? list.ProportionalElection.ContestId,
            PoliticalBusinessId = list.ProportionalElectionId,
            DomainOfInfluenceId = doiId ?? list.ProportionalElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionListUnionEvent<T>(T eventData, ProportionalElectionListUnion listUnion, Guid? contestId = null, Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = listUnion.Id,
            AggregateId = listUnion.ProportionalElectionId,
            ContestId = contestId ?? listUnion.ProportionalElection.ContestId,
            PoliticalBusinessId = listUnion.ProportionalElectionId,
            DomainOfInfluenceId = doiId ?? listUnion.ProportionalElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionCandidateEvent<T>(T eventData, ProportionalElectionCandidate candidate, ProportionalElectionList? list = null)
        where T : IMessage<T>
    {
        var proportionalElectionList = list ?? candidate.ProportionalElectionList;

        var eventLog = new EventLog
        {
            EntityId = candidate.Id,
            AggregateId = proportionalElectionList.ProportionalElectionId,
            ContestId = proportionalElectionList.ProportionalElection.ContestId,
            PoliticalBusinessId = proportionalElectionList.ProportionalElectionId,
            DomainOfInfluenceId = proportionalElectionList.ProportionalElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogProportionalElectionUnionEvent<T>(T eventData, ProportionalElectionUnion union, Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = union.Id,
            AggregateId = union.Id,
            ContestId = union.ContestId,
            PoliticalBusinessUnionId = union.Id,
            DomainOfInfluenceId = doiId ?? union.Contest.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogElectionGroupEvent<T>(T eventData, ElectionGroup electionGroup)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = electionGroup.Id,
            AggregateId = electionGroup.PrimaryMajorityElectionId,
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
            EntityId = ballotGroup.Id,
            AggregateId = ballotGroup.MajorityElectionId,
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
            EntityId = election.Id,
            AggregateId = election.Id,
            ContestId = election.ContestId,
            PoliticalBusinessId = election.Id,
            DomainOfInfluenceId = election.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogMajorityElectionCandidateEvent<T>(T eventData, MajorityElectionCandidate candidate, Guid? contestId = null, Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = candidate.Id,
            AggregateId = candidate.MajorityElectionId,
            ContestId = contestId ?? candidate.MajorityElection.ContestId,
            PoliticalBusinessId = candidate.MajorityElectionId,
            DomainOfInfluenceId = doiId ?? candidate.MajorityElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogSecondaryMajorityElectionEvent<T>(T eventData, SecondaryMajorityElection election)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = election.Id,
            AggregateId = election.PrimaryMajorityElectionId,
            ContestId = election.ContestId,
            PoliticalBusinessId = election.Id,
            DomainOfInfluenceId = election.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogSecondaryMajorityElectionCandidateEvent<T>(
        T eventData,
        SecondaryMajorityElectionCandidate candidate,
        Guid? primaryElectionId = null,
        Guid? contestId = null,
        Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = candidate.Id,
            AggregateId = primaryElectionId ?? candidate.SecondaryMajorityElection.PrimaryMajorityElectionId,
            ContestId = contestId ?? candidate.SecondaryMajorityElection.ContestId,
            PoliticalBusinessId = candidate.SecondaryMajorityElectionId,
            DomainOfInfluenceId = doiId ?? candidate.SecondaryMajorityElection.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }

    public async Task LogMajorityElectionUnionEvent<T>(T eventData, MajorityElectionUnion union, Guid? doiId = null)
        where T : IMessage<T>
    {
        var eventLog = new EventLog
        {
            EntityId = union.Id,
            AggregateId = union.Id,
            ContestId = union.ContestId,
            PoliticalBusinessUnionId = union.Id,
            DomainOfInfluenceId = doiId ?? union.Contest.DomainOfInfluenceId,
        };

        await _eventLogger.LogEvent(eventData, eventLog);
    }
}
