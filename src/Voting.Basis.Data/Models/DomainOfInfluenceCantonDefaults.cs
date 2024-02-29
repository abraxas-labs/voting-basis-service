// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class DomainOfInfluenceCantonDefaults
{
    public DomainOfInfluenceCanton Canton { get; set; }

    public List<ProportionalElectionMandateAlgorithm> ProportionalElectionMandateAlgorithms { get; set; }
        = new List<ProportionalElectionMandateAlgorithm>();

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public bool MajorityElectionInvalidVotes { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    public List<PoliticalBusinessUnionType> EnabledPoliticalBusinessUnionTypes { get; set; }
        = new List<PoliticalBusinessUnionType>();

    public bool MultipleVoteBallotsEnabled { get; set; }

    public bool ProportionalElectionUseCandidateCheckDigit { get; set; }

    public bool MajorityElectionUseCandidateCheckDigit { get; set; }
}
