// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class CantonSettings
{
    public Guid Id { get; set; }

    public DomainOfInfluenceCanton Canton { get; set; }

    /// <summary>
    /// Gets or sets the name of the authority which is responsible for this canton.
    /// </summary>
    public string AuthorityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the responsible tenant ID for this canton.
    /// </summary>
    public string SecureConnectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the allowed algorithm for proportional elections in this canton.
    /// </summary>
    public List<ProportionalElectionMandateAlgorithm>? ProportionalElectionMandateAlgorithms { get; set; }

    /// <summary>
    /// Gets or sets the default algorithm for majority elections for calculating the absolute majority.
    /// </summary>
    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether invalid votes are allowed in majority elections in this canton.
    /// </summary>
    public bool MajorityElectionInvalidVotes { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    public List<DomainOfInfluenceType>? SwissAbroadVotingRightDomainOfInfluenceTypes { get; set; }

    public List<PoliticalBusinessUnionType>? EnabledPoliticalBusinessUnionTypes { get; set; }

    public List<CantonSettingsVotingCardChannel>? EnabledVotingCardChannels { get; set; }

    public string VotingDocumentsEVotingEaiMessageType { get; set; } = string.Empty;

    public ProtocolDomainOfInfluenceSortType ProtocolDomainOfInfluenceSortType { get; set; }

    public ProtocolCountingCircleSortType ProtocolCountingCircleSortType { get; set; }
}
