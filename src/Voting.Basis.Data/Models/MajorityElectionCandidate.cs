// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class MajorityElectionCandidate : MajorityElectionCandidateBase
{
    public Guid MajorityElectionId { get; set; }

    public MajorityElection MajorityElection { get; set; } = null!;

    public ICollection<SecondaryMajorityElectionCandidate> CandidateReferences { get; set; } = new HashSet<SecondaryMajorityElectionCandidate>();

    public ICollection<MajorityElectionBallotGroupEntryCandidate> BallotGroupEntries { get; set; } = new HashSet<MajorityElectionBallotGroupEntryCandidate>();
}
