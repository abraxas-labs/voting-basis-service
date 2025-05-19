// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Extensions;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="PoliticalAssembly"/>.
/// </summary>
public sealed class PoliticalAssemblyAggregate : BaseDeletableAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<PoliticalAssembly> _validator;
    private readonly IClock _clock;

    public PoliticalAssemblyAggregate(IMapper mapper, EventInfoProvider eventInfoProvider, IValidator<PoliticalAssembly> validator, IClock clock)
    {
        Description = [];
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _clock = clock;
    }

    public override string AggregateName => "voting-political-assemblies";

    public DateTime Date { get; private set; }

    public Dictionary<string, string> Description { get; private set; }

    public Guid DomainOfInfluenceId { get; private set; }

    public PoliticalAssemblyState State { get; set; } = PoliticalAssemblyState.Active;

    public DateTime? ArchivePer { get; private set; }

    public DateTime PastLockPer { get; private set; }

    public void CreateFrom(PoliticalAssembly politicalAssembly)
    {
        if (politicalAssembly.Id == default)
        {
            politicalAssembly.Id = Guid.NewGuid();
        }

        _validator.ValidateAndThrow(politicalAssembly);
        NormalizeAndValidateDates(politicalAssembly);

        var ev = new PoliticalAssemblyCreated
        {
            PoliticalAssembly = _mapper.Map<PoliticalAssemblyEventData>(politicalAssembly),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateFrom(PoliticalAssembly politicalAssembly)
    {
        EnsureNotDeleted();
        _validator.ValidateAndThrow(politicalAssembly);
        NormalizeAndValidateDates(politicalAssembly);

        if (politicalAssembly.DomainOfInfluenceId != DomainOfInfluenceId)
        {
            throw new ValidationException($"{nameof(DomainOfInfluenceId)} is immutable.");
        }

        var ev = new PoliticalAssemblyUpdated
        {
            PoliticalAssembly = _mapper.Map<PoliticalAssemblyEventData>(politicalAssembly),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete()
    {
        EnsureNotDeleted();
        var ev = new PoliticalAssemblyDeleted
        {
            PoliticalAssemblyId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
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
            throw new ValidationException("Political assembly past lock per not yet reached");
        }

        var ev = new PoliticalAssemblyPastLocked
        {
            PoliticalAssemblyId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        return true;
    }

    public bool TryArchive()
    {
        if (State != PoliticalAssemblyState.PastLocked)
        {
            return false;
        }

        var ev = new PoliticalAssemblyArchived
        {
            PoliticalAssemblyId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        return true;
    }

    /// <summary>
    /// Archives the political assembly.
    /// </summary>
    /// <param name="archivePer">
    /// If null, the political assembly is archived immediately.
    /// Otherwise the archival is scheduled for the specified date.
    /// </param>
    /// <exception cref="ValidationException">
    /// If the provided date is not in the future or not after the political assembly date.
    /// If no archivePer is provided and the political assembly is not in the past yet.
    /// </exception>
    public void Archive(DateTime? archivePer = null)
    {
        if (State != PoliticalAssemblyState.PastLocked)
        {
            throw new ValidationException("a political assembly can only be archived if it is in the past state");
        }

        if (!archivePer.HasValue)
        {
            var archivedEvent = new PoliticalAssemblyArchived
            {
                PoliticalAssemblyId = Id.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            };

            RaiseEvent(archivedEvent);
            return;
        }

        if (archivePer < Date || archivePer < _clock.UtcNow)
        {
            throw new ValidationException("archive per has to be after the political assembly date and in the future");
        }

        var ev = new PoliticalAssemblyArchiveDateUpdated
        {
            PoliticalAssemblyId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ArchivePer = archivePer.Value.ToTimestamp(),
        };
        RaiseEvent(ev);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case PoliticalAssemblyCreated e:
                Apply(e);
                break;
            case PoliticalAssemblyUpdated e:
                Apply(e);
                break;
            case PoliticalAssemblyDeleted _:
                Deleted = true;
                break;
            case PoliticalAssemblyArchived e:
                Apply(e);
                break;
            case PoliticalAssemblyPastLocked _:
                State = PoliticalAssemblyState.PastLocked;
                break;
        }
    }

    private void RaiseEvent(IMessage eventData)
    {
        RaiseEvent(eventData, EventSignatureBusinessMetadataBuilder.BuildFrom(Id));
    }

    private void Apply(PoliticalAssemblyArchived e)
    {
        // the date of the event can be before the archive per date
        // if an archive date is set in the future but the user selects archive now.
        var eventDate = e.EventInfo.Timestamp.ToDateTime();
        if (ArchivePer == null || ArchivePer > eventDate)
        {
            ArchivePer = eventDate;
        }

        State = PoliticalAssemblyState.Archived;
    }

    private void Apply(PoliticalAssemblyCreated ev)
    {
        _mapper.Map(ev.PoliticalAssembly, this);
    }

    private void Apply(PoliticalAssemblyUpdated ev)
    {
        _mapper.Map(ev.PoliticalAssembly, this);
    }

    private void NormalizeAndValidateDates(PoliticalAssembly politicalAssembly)
    {
        politicalAssembly.Date = politicalAssembly.Date.Date;

        if (politicalAssembly.Date < _clock.UtcNow)
        {
            throw new ValidationException("The date must be in the future");
        }
    }
}
