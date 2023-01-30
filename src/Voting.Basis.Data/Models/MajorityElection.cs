// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class MajorityElection : PoliticalBusiness
{
    public int NumberOfMandates { get; set; }

    public MajorityElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public bool CandidateCheckDigit { get; set; }

    public int BallotBundleSize { get; set; }

    public int BallotBundleSampleSize { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public BallotNumberGeneration BallotNumberGeneration { get; set; }

    public bool AutomaticEmptyVoteCounting { get; set; }

    public bool EnforceEmptyVoteCountingForCountingCircles { get; set; }

    public MajorityElectionResultEntry ResultEntry { get; set; }

    public bool EnforceResultEntryForCountingCircles { get; set; }

    public int ReportDomainOfInfluenceLevel { get; set; }

    public override PoliticalBusinessType PoliticalBusinessType => PoliticalBusinessType.MajorityElection;

    public ICollection<MajorityElectionCandidate> MajorityElectionCandidates { get; set; } = new HashSet<MajorityElectionCandidate>();

    public ICollection<SecondaryMajorityElection> SecondaryMajorityElections { get; set; } = new HashSet<SecondaryMajorityElection>();

    public ElectionGroup? ElectionGroup { get; set; }

    public ICollection<MajorityElectionBallotGroup> BallotGroups { get; set; } = new HashSet<MajorityElectionBallotGroup>();

    public ICollection<MajorityElectionBallotGroupEntry> BallotGroupEntries { get; set; } = new HashSet<MajorityElectionBallotGroupEntry>();

    public ICollection<MajorityElectionUnionEntry> MajorityElectionUnionEntries { get; set; } = new HashSet<MajorityElectionUnionEntry>();

    public MajorityElectionReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }
}
