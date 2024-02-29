// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class SecondaryMajorityElectionCandidate : MajorityElectionCandidateBase
{
    public Guid SecondaryMajorityElectionId { get; set; }

    public SecondaryMajorityElection SecondaryMajorityElection { get; set; } = null!;

    public Guid? CandidateReferenceId { get; set; }

    public MajorityElectionCandidate? CandidateReference { get; set; }

    public ICollection<MajorityElectionBallotGroupEntryCandidate> BallotGroupEntries { get; set; } = new HashSet<MajorityElectionBallotGroupEntryCandidate>();
}
