// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Google.Protobuf;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// A proportional election union (in german: Geschäftsverbindung zwischen Proporzwahlen).
/// </summary>
public class ProportionalElectionUnionAggregate : BaseHasContestAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public ProportionalElectionUnionAggregate(
        EventInfoProvider eventInfoProvider,
        IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;

        Description = string.Empty;
        SecureConnectId = string.Empty;
    }

    public override string AggregateName => "voting-proportionalElectionUnions";

    public string Description { get; private set; }

    public string SecureConnectId { get; private set; }

    public void CreateFrom(ProportionalElectionUnion proportionalElectionUnion)
    {
        if (proportionalElectionUnion.Id == default)
        {
            proportionalElectionUnion.Id = Guid.NewGuid();
        }

        var ev = new ProportionalElectionUnionCreated
        {
            ProportionalElectionUnion = _mapper.Map<ProportionalElectionUnionEventData>(proportionalElectionUnion),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(proportionalElectionUnion.ContestId));
    }

    public void UpdateFrom(ProportionalElectionUnion proportionalElectionUnion)
    {
        EnsureNotDeleted();

        // ContestId and SecureConnectId should never change
        proportionalElectionUnion.ContestId = ContestId;
        proportionalElectionUnion.SecureConnectId = SecureConnectId;

        var ev = new ProportionalElectionUnionUpdated
        {
            ProportionalElectionUnion = _mapper.Map<ProportionalElectionUnionEventData>(proportionalElectionUnion),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete()
    {
        EnsureNotDeleted();
        var ev = new ProportionalElectionUnionDeleted
        {
            ProportionalElectionUnionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateEntries(IReadOnlyCollection<Guid> proportionalElectionIds)
    {
        EnsureNotDeleted();
        var ev = new ProportionalElectionUnionEntriesUpdated
        {
            ProportionalElectionUnionEntries = new ProportionalElectionUnionEntriesEventData
            {
                ProportionalElectionUnionId = Id.ToString(),
                ProportionalElectionIds = { proportionalElectionIds.Select(x => x.ToString()) },
            },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public override void MoveToNewContest(Guid newContestId)
    {
        EnsureNotDeleted();
        EnsureDifferentContest(newContestId);

        var ev = new ProportionalElectionUnionToNewContestMoved
        {
            ProportionalElectionUnionId = Id.ToString(),
            NewContestId = newContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(newContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionUnionCreated e:
                Apply(e);
                break;
            case ProportionalElectionUnionUpdated e:
                Apply(e);
                break;
            case ProportionalElectionUnionEntriesUpdated _:
                break;
            case ProportionalElectionUnionDeleted _:
                Deleted = true;
                break;
            case ProportionalElectionUnionToNewContestMoved e:
                ContestId = GuidParser.Parse(e.NewContestId);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ProportionalElectionUnionCreated ev)
    {
        _mapper.Map(ev.ProportionalElectionUnion, this);
    }

    private void Apply(ProportionalElectionUnionUpdated ev)
    {
        _mapper.Map(ev.ProportionalElectionUnion, this);
    }
}
