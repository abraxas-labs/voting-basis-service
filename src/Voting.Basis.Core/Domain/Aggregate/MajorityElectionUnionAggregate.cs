// (c) Copyright by Abraxas Informatik AG
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
/// A majority election union (in german: Geschäftsverbindung zwischen Majorzwahlen).
/// </summary>
public class MajorityElectionUnionAggregate : BaseHasContestAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public MajorityElectionUnionAggregate(
        EventInfoProvider eventInfoProvider,
        IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;

        Description = string.Empty;
        SecureConnectId = string.Empty;
    }

    public override string AggregateName => "voting-majorityElectionUnions";

    public string Description { get; private set; }

    public string SecureConnectId { get; private set; }

    public void CreateFrom(MajorityElectionUnion majorityElectionUnion)
    {
        if (majorityElectionUnion.Id == default)
        {
            majorityElectionUnion.Id = Guid.NewGuid();
        }

        var ev = new MajorityElectionUnionCreated
        {
            MajorityElectionUnion = _mapper.Map<MajorityElectionUnionEventData>(majorityElectionUnion),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(majorityElectionUnion.ContestId));
    }

    public void UpdateFrom(MajorityElectionUnion majorityElectionUnion)
    {
        EnsureNotDeleted();

        // ContestId and SecureConnectId should never change
        majorityElectionUnion.ContestId = ContestId;
        majorityElectionUnion.SecureConnectId = SecureConnectId;

        var ev = new MajorityElectionUnionUpdated
        {
            MajorityElectionUnion = _mapper.Map<MajorityElectionUnionEventData>(majorityElectionUnion),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete()
    {
        EnsureNotDeleted();
        var ev = new MajorityElectionUnionDeleted
        {
            MajorityElectionUnionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateEntries(List<Guid> majorityElectionIds)
    {
        EnsureNotDeleted();
        var ev = new MajorityElectionUnionEntriesUpdated
        {
            MajorityElectionUnionEntries = new MajorityElectionUnionEntriesEventData
            {
                MajorityElectionUnionId = Id.ToString(),
                MajorityElectionIds = { majorityElectionIds.Select(x => x.ToString()) },
            },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public override void MoveToNewContest(Guid newContestId)
    {
        EnsureNotDeleted();
        EnsureDifferentContest(newContestId);

        var ev = new MajorityElectionUnionToNewContestMoved
        {
            MajorityElectionUnionId = Id.ToString(),
            NewContestId = newContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(newContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case MajorityElectionUnionCreated e:
                Apply(e);
                break;
            case MajorityElectionUnionUpdated e:
                Apply(e);
                break;
            case MajorityElectionUnionEntriesUpdated _:
                break;
            case MajorityElectionUnionDeleted _:
                Deleted = true;
                break;
            case MajorityElectionUnionToNewContestMoved e:
                ContestId = GuidParser.Parse(e.NewContestId);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(MajorityElectionUnionCreated ev)
    {
        _mapper.Map(ev.MajorityElectionUnion, this);
    }

    private void Apply(MajorityElectionUnionUpdated ev)
    {
        _mapper.Map(ev.MajorityElectionUnion, this);
    }
}
