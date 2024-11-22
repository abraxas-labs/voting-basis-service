// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="CantonSettings"/>.
/// </summary>
public class CantonSettingsAggregate : BaseEventSourcingAggregate
{
    private readonly IValidator<CantonSettings> _validator;
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;

    public CantonSettingsAggregate(
        IValidator<CantonSettings> validator,
        IMapper mapper,
        EventInfoProvider eventInfoProvider)
    {
        _validator = validator;
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;

        AuthorityName = string.Empty;
        SecureConnectId = string.Empty;
        ProportionalElectionMandateAlgorithms = new List<ProportionalElectionMandateAlgorithm>();
        SwissAbroadVotingRightDomainOfInfluenceTypes = new List<DomainOfInfluenceType>();
        VotingDocumentsEVotingEaiMessageType = string.Empty;
    }

    public override string AggregateName => "voting-cantonSettings";

    public DomainOfInfluenceCanton Canton { get; private set; }

    public string AuthorityName { get; private set; }

    public string SecureConnectId { get; private set; }

    public IEnumerable<ProportionalElectionMandateAlgorithm> ProportionalElectionMandateAlgorithms { get; private set; }

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; private set; }

    public bool MajorityElectionInvalidVotes { get; private set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; private set; }

    public IEnumerable<DomainOfInfluenceType> SwissAbroadVotingRightDomainOfInfluenceTypes { get; private set; }

    public string VotingDocumentsEVotingEaiMessageType { get; private set; }

    public ProtocolDomainOfInfluenceSortType ProtocolDomainOfInfluenceSortType { get; private set; }

    public ProtocolCountingCircleSortType ProtocolCountingCircleSortType { get; private set; }

    public bool MultipleVoteBallotsEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether counting circles in VOTING Ausmittlung can use counting machines or not.
    /// </summary>
    public bool CountingMachineEnabled { get; private set; }

    public bool ProportionalElectionUseCandidateCheckDigit { get; private set; }

    public bool MajorityElectionUseCandidateCheckDigit { get; private set; }

    public bool CreateContestOnHighestHierarchicalLevelEnabled { get; private set; }

    public bool InternalPlausibilisationDisabled { get; private set; }

    public bool PublishResultsBeforeAuditedTentatively { get; private set; }

    public void CreateFrom(CantonSettings cantonSettings)
    {
        cantonSettings.Id = BasisUuidV5.BuildCantonSettings(cantonSettings.Canton);
        _validator.ValidateAndThrow(cantonSettings);

        var ev = new CantonSettingsCreated
        {
            CantonSettings = _mapper.Map<CantonSettingsEventData>(cantonSettings),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateFrom(CantonSettings cantonSettings)
    {
        _validator.ValidateAndThrow(cantonSettings);

        // The canton should never be changed
        cantonSettings.Canton = Canton;

        var ev = new CantonSettingsUpdated
        {
            CantonSettings = _mapper.Map<CantonSettingsEventData>(cantonSettings),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case CantonSettingsCreated e:
                Apply(e);
                break;
            case CantonSettingsUpdated e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(CantonSettingsCreated ev)
    {
        Migrate(ev.CantonSettings);
        _mapper.Map(ev.CantonSettings, this);
    }

    private void Apply(CantonSettingsUpdated ev)
    {
        Migrate(ev.CantonSettings);
        _mapper.Map(ev.CantonSettings, this);
    }

    private void Migrate(CantonSettingsEventData eventData)
    {
        // Set default sort type value since the old eventData (before introducing the sort type) can contain the unspecified value.
        if (eventData.ProtocolCountingCircleSortType == Abraxas.Voting.Basis.Shared.V1.ProtocolCountingCircleSortType.Unspecified)
        {
            eventData.ProtocolCountingCircleSortType = Abraxas.Voting.Basis.Shared.V1.ProtocolCountingCircleSortType.SortNumber;
        }

        // Set default sort type value since the old eventData (before introducing the sort type) can contain the unspecified value.
        if (eventData.ProtocolDomainOfInfluenceSortType == Abraxas.Voting.Basis.Shared.V1.ProtocolDomainOfInfluenceSortType.Unspecified)
        {
            eventData.ProtocolDomainOfInfluenceSortType = Abraxas.Voting.Basis.Shared.V1.ProtocolDomainOfInfluenceSortType.SortNumber;
        }
    }
}
