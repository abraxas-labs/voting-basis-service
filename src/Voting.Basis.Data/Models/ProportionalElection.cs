// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class ProportionalElection : PoliticalBusiness
{
    public int NumberOfMandates { get; set; }

    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public bool CandidateCheckDigit { get; set; }

    public int BallotBundleSize { get; set; }

    public int BallotBundleSampleSize { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public BallotNumberGeneration BallotNumberGeneration { get; set; }

    public bool AutomaticEmptyVoteCounting { get; set; }

    public bool EnforceEmptyVoteCountingForCountingCircles { get; set; }

    public override PoliticalBusinessType PoliticalBusinessType => PoliticalBusinessType.ProportionalElection;

    public override PoliticalBusinessSubType PoliticalBusinessSubType => PoliticalBusinessSubType.Unspecified;

    public ICollection<ProportionalElectionList> ProportionalElectionLists { get; set; } = new HashSet<ProportionalElectionList>();

    public ICollection<ProportionalElectionListUnion> ProportionalElectionListUnions { get; set; } = new HashSet<ProportionalElectionListUnion>();

    public ICollection<ProportionalElectionUnionEntry> ProportionalElectionUnionEntries { get; set; } = new HashSet<ProportionalElectionUnionEntry>();

    public ProportionalElectionReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public bool EnforceCandidateCheckDigitForCountingCircles { get; set; }

    public int? FederalIdentification { get; set; }
}
