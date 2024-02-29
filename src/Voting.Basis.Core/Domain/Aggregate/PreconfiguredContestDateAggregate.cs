// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentValidation;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// The preconfigured contest date aggregate (in german: Blanko-Abstimmungstermine).
/// </summary>
public sealed class PreconfiguredContestDateAggregate : BaseEventSourcingAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public PreconfiguredContestDateAggregate(EventInfoProvider eventInfoProvider)
    {
        PreconfiguredDates = new List<DateTime>();
        _eventInfoProvider = eventInfoProvider;
        Id = new Guid("31ece920-cd42-440f-a6d5-cd9c218835cd");
    }

    public override string AggregateName => "voting-preconfiguredContestDates";

    public override string StreamName => AggregateName;

    private List<DateTime> PreconfiguredDates { get; }

    public void AddDate(DateTime date)
    {
        var normalizedDate = date.Date;
        if (PreconfiguredDates.Contains(normalizedDate))
        {
            throw new ValidationException($"{nameof(date)} already exists.");
        }

        var ev = new PreconfiguredContestDateCreated
        {
            PreconfiguredContestDate = new PreconfiguredContestDateEventData
            {
                Date = normalizedDate.ToTimestamp(),
            },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case PreconfiguredContestDateCreated e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(PreconfiguredContestDateCreated ev)
    {
        PreconfiguredDates.Add(ev.PreconfiguredContestDate.Date.ToDateTime());
    }
}
