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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.VotingExports.Repository;
using ExportProvider = Abraxas.Voting.Basis.Shared.V1.ExportProvider;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="DomainOfInfluence"/>.
/// </summary>
public sealed class DomainOfInfluenceAggregate : BaseDeletableAggregate
{
    private const string LogoRefFormat = "v1/{0}";

    private readonly List<string> _countingCircles;
    private readonly List<ExportConfiguration> _exportConfigurations = new();
    private readonly List<DomainOfInfluenceParty> _parties;
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<DomainOfInfluence> _validator;
    private readonly IValidator<DomainOfInfluenceVotingCardPrintData> _printDataValidator;
    private readonly IValidator<PlausibilisationConfiguration> _plausiConfigValidator;

    public DomainOfInfluenceAggregate(
        IMapper mapper,
        EventInfoProvider eventInfoProvider,
        IValidator<DomainOfInfluence> validator,
        IValidator<DomainOfInfluenceVotingCardPrintData> printDataValidator,
        IValidator<PlausibilisationConfiguration> plausiConfigValidator)
    {
        Name = string.Empty;
        ShortName = string.Empty;
        SecureConnectId = string.Empty;
        AuthorityName = string.Empty;
        ExternalPrintingCenterEaiMessageType = string.Empty;
        SapCustomerOrderNumber = string.Empty;
        NameForProtocol = string.Empty;
        ContactPerson = new ContactPerson();
        PlausibilisationConfiguration = new PlausibilisationConfiguration();
        _parties = new List<DomainOfInfluenceParty>();
        _countingCircles = new List<string>();
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _printDataValidator = printDataValidator;
        _plausiConfigValidator = plausiConfigValidator;
    }

    public override string AggregateName => "voting-domainOfInfluences";

    public string Name { get; private set; }

    public string ShortName { get; private set; }

    public string SecureConnectId { get; private set; }

    public string AuthorityName { get; private set; }

    public int SortNumber { get; private set; }

    public string NameForProtocol { get; private set; }

    public ContactPerson ContactPerson { get; private set; }

    public DomainOfInfluenceType Type { get; private set; }

    public DomainOfInfluenceCanton Canton { get; private set; }

    public bool ResponsibleForVotingCards { get; private set; }

    public DomainOfInfluenceVotingCardReturnAddress? ReturnAddress { get; private set; }

    public DomainOfInfluenceVotingCardPrintData? PrintData { get; private set; }

    public DomainOfInfluenceVotingCardSwissPostData? SwissPostData { get; private set; }

    public PlausibilisationConfiguration PlausibilisationConfiguration { get; private set; }

    public Guid? ParentId { get; private set; }

    public bool ExternalPrintingCenter { get; private set; }

    public string ExternalPrintingCenterEaiMessageType { get; private set; }

    public string SapCustomerOrderNumber { get; private set; }

    public string? LogoRef { get; private set; }

    public bool HasLogo => LogoRef != null;

    public IEnumerable<string> CountingCircles => _countingCircles.AsEnumerable();

    public bool VirtualTopLevel { get; private set; }

    public bool ViewCountingCirclePartialResults { get; private set; }

    /// <summary>
    /// Gets a value indicating whether VOTING Stimmregister is enabled.
    /// </summary>
    public bool ElectoralRegistrationEnabled { get; private set; }

    public void CreateFrom(DomainOfInfluence domainOfInfluence)
    {
        if (domainOfInfluence.Id == default)
        {
            domainOfInfluence.Id = Guid.NewGuid();
        }

        _validator.ValidateAndThrow(domainOfInfluence);
        ValidateCanton(domainOfInfluence);

        var ev = new DomainOfInfluenceCreated
        {
            DomainOfInfluence = _mapper.Map<DomainOfInfluenceEventData>(domainOfInfluence),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        UpdateChildrenFrom(domainOfInfluence);
    }

    public void UpdateFrom(DomainOfInfluence domainOfInfluence)
    {
        EnsureNotDeleted();
        _validator.ValidateAndThrow(domainOfInfluence);
        ValidateCanton(domainOfInfluence);

        if (domainOfInfluence.Type != Type)
        {
            throw new ValidationException($"{nameof(Type)} is immutable.");
        }

        // parent id should never be changed
        domainOfInfluence.ParentId = ParentId;

        var ev = new DomainOfInfluenceUpdated
        {
            DomainOfInfluence = _mapper.Map<DomainOfInfluenceEventData>(domainOfInfluence),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
        UpdateChildrenFrom(domainOfInfluence);
    }

    public void Delete()
    {
        EnsureNotDeleted();
        var ev = new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCountingCircleEntries(DomainOfInfluenceCountingCircleEntries countingCircleEntries)
    {
        EnsureNotDeleted();
        var ev = new DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = _mapper.Map<DomainOfInfluenceCountingCircleEntriesEventData>(countingCircleEntries),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateContactPerson(ContactPerson contactPerson)
    {
        EnsureNotDeleted();

        var ev = new DomainOfInfluenceContactPersonUpdated
        {
            DomainOfInfluenceId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ContactPerson = _mapper.Map<ContactPersonEventData>(contactPerson),
        };

        RaiseEvent(ev);
    }

    public void UpdateVotingCardData(
        DomainOfInfluenceVotingCardReturnAddress returnAddress,
        DomainOfInfluenceVotingCardPrintData printData,
        bool externalPrintingCenter,
        string externalPrintingCenterEaiMessageType,
        string sapCustomerOrderNumber,
        DomainOfInfluenceVotingCardSwissPostData? swissPostData,
        VotingCardColor votingCardColor)
    {
        EnsureNotDeleted();

        if (!ResponsibleForVotingCards)
        {
            throw new ValidationException($"updating voting card data is not allowed when {nameof(ResponsibleForVotingCards)} is false");
        }

        if (externalPrintingCenter && string.IsNullOrWhiteSpace(externalPrintingCenterEaiMessageType))
        {
            throw new ValidationException($"{nameof(ExternalPrintingCenterEaiMessageType)} may not be empty when {nameof(ExternalPrintingCenter)} is active");
        }

        _printDataValidator.ValidateAndThrow(printData);

        var ev = new DomainOfInfluenceVotingCardDataUpdated
        {
            DomainOfInfluenceId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ReturnAddress = _mapper.Map<DomainOfInfluenceVotingCardReturnAddressEventData>(returnAddress),
            PrintData = _mapper.Map<DomainOfInfluenceVotingCardPrintDataEventData>(printData),
            SwissPostData = _mapper.Map<DomainOfInfluenceVotingCardSwissPostDataEventData>(swissPostData ?? SwissPostData),
            ExternalPrintingCenter = externalPrintingCenter,
            ExternalPrintingCenterEaiMessageType = externalPrintingCenterEaiMessageType,
            SapCustomerOrderNumber = sapCustomerOrderNumber,
            VotingCardColor = _mapper.Map<SharedProto.VotingCardColor>(votingCardColor),
        };

        RaiseEvent(ev);
    }

    public void UpdateLogo()
    {
        EnsureNotDeleted();

        var ev = new DomainOfInfluenceLogoUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            LogoRef = string.Format(LogoRefFormat, Id.ToString()),
            DomainOfInfluenceId = Id.ToString(),
        };

        RaiseEvent(ev);
    }

    public void DeleteLogo()
    {
        EnsureNotDeleted();

        if (!HasLogo)
        {
            throw new ValidationException("cannot delete logo if no logo reference is set");
        }

        var ev = new DomainOfInfluenceLogoDeleted
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            LogoRef = LogoRef,
            DomainOfInfluenceId = Id.ToString(),
        };

        RaiseEvent(ev);
    }

    public void UpdatePlausibilisationConfiguration(PlausibilisationConfiguration plausiConfig)
    {
        EnsureNotDeleted();

        _plausiConfigValidator.ValidateAndThrow(plausiConfig);

        var ev = new DomainOfInfluencePlausibilisationConfigurationUpdated
        {
            DomainOfInfluenceId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            PlausibilisationConfiguration = _mapper.Map<PlausibilisationConfigurationEventData>(plausiConfig),
        };

        RaiseEvent(ev);
    }

    public void UpdateParties(IReadOnlyCollection<DomainOfInfluenceParty> parties)
    {
        SyncParties(parties);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case DomainOfInfluenceCreated e:
                Apply(e);
                break;
            case DomainOfInfluenceUpdated e:
                Apply(e);
                break;
            case DomainOfInfluenceCountingCircleEntriesUpdated e:
                Apply(e);
                break;
            case DomainOfInfluenceContactPersonUpdated e:
                Apply(e);
                break;
            case DomainOfInfluenceDeleted _:
                Deleted = true;
                break;
            case ExportConfigurationCreated ev:
                Apply(ev);
                break;
            case ExportConfigurationUpdated ev:
                Apply(ev);
                break;
            case ExportConfigurationDeleted ev:
                Apply(ev);
                break;
            case DomainOfInfluenceVotingCardDataUpdated ev:
                Apply(ev);
                break;
            case DomainOfInfluenceLogoDeleted:
                LogoRef = null;
                break;
            case DomainOfInfluenceLogoUpdated ev:
                LogoRef = ev.LogoRef;
                break;
            case DomainOfInfluencePlausibilisationConfigurationUpdated ev:
                Apply(ev);
                break;
            case DomainOfInfluencePartyCreated ev:
                Apply(ev);
                break;
            case DomainOfInfluencePartyUpdated ev:
                Apply(ev);
                break;
            case DomainOfInfluencePartyDeleted ev:
                Apply(ev);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void SyncExportConfigurations(IEnumerable<ExportConfiguration> updatedExportConfigurations)
    {
        var updatedConfigs = _mapper.Map<List<ExportConfiguration>>(updatedExportConfigurations);

        if (updatedConfigs.Count != updatedConfigs.DistinctBy(p => p.Id).Count())
        {
            throw new ValidationException("each export configuration can only be provided exactly once");
        }

        // Ensure Templates exists
        foreach (var key in updatedConfigs.SelectMany(x => x.ExportKeys))
        {
            TemplateRepository.GetByKey(key);
        }

        var diff = _exportConfigurations.BuildDiff(updatedConfigs, x => x.Id);
        foreach (var removed in diff.Removed)
        {
            RaiseEvent(new ExportConfigurationDeleted
            {
                ConfigurationId = removed.Id.ToString(),
                DomainOfInfluenceId = Id.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            });
        }

        foreach (var modified in diff.Modified)
        {
            var modifiedConfig = _mapper.Map<ExportConfiguration>(modified);
            modifiedConfig.DomainOfInfluenceId = Id;
            RaiseEvent(new ExportConfigurationUpdated
            {
                Configuration = _mapper.Map<ExportConfigurationEventData>(modifiedConfig),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            });
        }

        foreach (var added in diff.Added)
        {
            var addedConfig = _mapper.Map<ExportConfiguration>(added);
            addedConfig.DomainOfInfluenceId = Id;
            RaiseEvent(new ExportConfigurationCreated
            {
                Configuration = _mapper.Map<ExportConfigurationEventData>(addedConfig),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            });
        }
    }

    private void SyncParties(IReadOnlyCollection<DomainOfInfluenceParty> updatedParties)
    {
        foreach (var party in updatedParties)
        {
            if (party.Id == default)
            {
                party.Id = Guid.NewGuid();
            }
        }

        if (updatedParties.Count != updatedParties.DistinctBy(p => p.Id).Count())
        {
            throw new ValidationException("each domain of influence party can only be provided exactly once");
        }

        var diff = _parties.BuildDiff(
            _mapper.Map<List<DomainOfInfluenceParty>>(updatedParties),
            x => x.Id);

        foreach (var removed in diff.Removed)
        {
            RaiseEvent(new DomainOfInfluencePartyDeleted
            {
                Id = removed.Id.ToString(),
                DomainOfInfluenceId = Id.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            });
        }

        foreach (var modified in diff.Modified)
        {
            modified.DomainOfInfluenceId = Id;
            RaiseEvent(new DomainOfInfluencePartyUpdated
            {
                Party = _mapper.Map<DomainOfInfluencePartyEventData>(modified),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            });
        }

        foreach (var added in diff.Added)
        {
            added.DomainOfInfluenceId = Id;
            RaiseEvent(new DomainOfInfluencePartyCreated
            {
                Party = _mapper.Map<DomainOfInfluencePartyEventData>(added),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            });
        }
    }

    private void Apply(DomainOfInfluenceCreated ev)
    {
        _mapper.Map(ev.DomainOfInfluence, this);
    }

    private void Apply(DomainOfInfluenceUpdated ev)
    {
        _mapper.Map(ev.DomainOfInfluence, this);
    }

    private void Apply(DomainOfInfluenceCountingCircleEntriesUpdated ev)
    {
        _countingCircles.Clear();
        _countingCircles.AddRange(ev.DomainOfInfluenceCountingCircleEntries.CountingCircleIds);
    }

    private void Apply(DomainOfInfluenceContactPersonUpdated ev)
    {
        _mapper.Map(ev.ContactPerson, ContactPerson);
    }

    private void Apply(ExportConfigurationCreated ev)
    {
        // Adjust "old" events that were created before the event provider was implemented
        if (ev.Configuration.Provider == ExportProvider.Unspecified)
        {
            ev.Configuration.Provider = ExportProvider.Standard;
        }

        var config = _mapper.Map<ExportConfiguration>(ev.Configuration);
        _exportConfigurations.Add(config);
    }

    private void Apply(ExportConfigurationUpdated ev)
    {
        // Adjust "old" events that were created before the event provider was implemented
        if (ev.Configuration.Provider == ExportProvider.Unspecified)
        {
            ev.Configuration.Provider = ExportProvider.Standard;
        }

        var config = _mapper.Map<ExportConfiguration>(ev.Configuration);
        _exportConfigurations.RemoveAll(x => x.Id == config.Id);
        _exportConfigurations.Add(config);
    }

    private void Apply(ExportConfigurationDeleted ev)
    {
        var id = GuidParser.Parse(ev.ConfigurationId);
        _exportConfigurations.RemoveAll(x => x.Id == id);
    }

    private void Apply(DomainOfInfluenceVotingCardDataUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(DomainOfInfluencePlausibilisationConfigurationUpdated ev)
    {
        PlausibilisationConfiguration = _mapper.Map<PlausibilisationConfiguration>(ev.PlausibilisationConfiguration);
    }

    private void Apply(DomainOfInfluencePartyCreated ev)
    {
        _parties.Add(_mapper.Map<DomainOfInfluenceParty>(ev.Party));
    }

    private void Apply(DomainOfInfluencePartyUpdated ev)
    {
        var party = _mapper.Map<DomainOfInfluenceParty>(ev.Party);
        _parties.RemoveAll(x => x.Id == party.Id);
        _parties.Add(party);
    }

    private void Apply(DomainOfInfluencePartyDeleted ev)
    {
        var partyId = GuidParser.Parse(ev.Id);
        _parties.RemoveAll(x => x.Id == partyId);
    }

    private void ValidateCanton(DomainOfInfluence doi)
    {
        // Update doesn't provide a ParentId, so we have to get it from the Aggregate
        var parentId = doi.ParentId ?? ParentId;
        var isRoot = !parentId.HasValue;

        if (isRoot && doi.Canton == DomainOfInfluenceCanton.Unspecified)
        {
            throw new ValidationException("A Root DomainOfInfluence must have a Canton");
        }

        if (!isRoot)
        {
            // canton is implicitly set on all dois since it is inherited by the root doi,
            // therefore it may also be included in create/update calls,
            // but is only taken into account if the doi is a root doi.
            doi.Canton = DomainOfInfluenceCanton.Unspecified;
        }
    }

    private void UpdateChildrenFrom(DomainOfInfluence domainOfInfluence)
    {
        UpdateContactPerson(domainOfInfluence.ContactPerson);
        UpdatePlausibilisationConfiguration(domainOfInfluence.PlausibilisationConfiguration ?? throw new ValidationException(nameof(domainOfInfluence.PlausibilisationConfiguration) + " must be set"));
        SyncExportConfigurations(domainOfInfluence.ExportConfigurations);
        SyncParties(domainOfInfluence.Parties);

        if (domainOfInfluence.ResponsibleForVotingCards)
        {
            UpdateVotingCardData(
                domainOfInfluence.ReturnAddress ?? throw new ValidationException(nameof(domainOfInfluence.ReturnAddress) + " must be set"),
                domainOfInfluence.PrintData ?? throw new ValidationException(nameof(domainOfInfluence.PrintData) + " must be set"),
                domainOfInfluence.ExternalPrintingCenter,
                domainOfInfluence.ExternalPrintingCenterEaiMessageType,
                domainOfInfluence.SapCustomerOrderNumber,
                domainOfInfluence.SwissPostData ?? throw new ValidationException(nameof(domainOfInfluence.SwissPostData) + " must be set"),
                domainOfInfluence.VotingCardColor);
        }
    }
}
