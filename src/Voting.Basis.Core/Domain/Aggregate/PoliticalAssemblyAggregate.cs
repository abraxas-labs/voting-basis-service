// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Basis.Core.Utils;
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
        Description = new Dictionary<string, string>();
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _clock = clock;
    }

    public override string AggregateName => "voting-political-assemblies";

    public DateTime Date { get; private set; }

    public Dictionary<string, string> Description { get; private set; }

    public Guid DomainOfInfluenceId { get; private set; }

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
        }
    }

    private void RaiseEvent(IMessage eventData)
    {
        RaiseEvent(eventData, EventSignatureBusinessMetadataBuilder.BuildFrom(Id));
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
