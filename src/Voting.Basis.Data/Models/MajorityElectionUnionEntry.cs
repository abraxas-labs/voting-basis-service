// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models;

public class MajorityElectionUnionEntry : PoliticalBusinessUnionEntry
{
    public Guid MajorityElectionUnionId { get; set; }

    public MajorityElectionUnion MajorityElectionUnion { get; set; } = null!; // set by ef

    public Guid MajorityElectionId { get; set; }

    public MajorityElection MajorityElection { get; set; } = null!; // set by ef
}
