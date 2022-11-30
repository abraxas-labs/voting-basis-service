// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class CantonSettings : BaseEntity
{
    public DomainOfInfluenceCanton Canton { get; set; }

    public string SecureConnectId { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public List<ProportionalElectionMandateAlgorithm> ProportionalElectionMandateAlgorithms { get; set; }
        = new List<ProportionalElectionMandateAlgorithm>();

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public bool MajorityElectionInvalidVotes { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    public List<DomainOfInfluenceType> SwissAbroadVotingRightDomainOfInfluenceTypes { get; set; }
        = new List<DomainOfInfluenceType>();

    public List<PoliticalBusinessUnionType> EnabledPoliticalBusinessUnionTypes { get; set; }
        = new List<PoliticalBusinessUnionType>();

    public List<CantonSettingsVotingCardChannel> EnabledVotingCardChannels { get; set; }
        = new List<CantonSettingsVotingCardChannel>();

    public string VotingDocumentsEVotingEaiMessageType { get; set; } = string.Empty;

    public ProtocolDomainOfInfluenceSortType ProtocolDomainOfInfluenceSortType { get; set; }

    public ProtocolCountingCircleSortType ProtocolCountingCircleSortType { get; set; }
}
