// (c) Copyright by Abraxas Informatik AG
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

    public bool CreateContestOnHighestHierarchicalLevelEnabled { get; set; }

    public bool InternalPlausibilisationDisabled { get; set; }

    public bool CandidateLocalityRequired { get; set; }

    public bool CandidateOriginRequired { get; set; }

    public bool DomainOfInfluencePublishResultsOptionEnabled { get; set; }

    public bool SecondaryMajorityElectionOnSeparateBallot { get; set; }
}
