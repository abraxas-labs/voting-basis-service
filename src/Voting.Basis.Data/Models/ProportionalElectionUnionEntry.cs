// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models;

public class ProportionalElectionUnionEntry : PoliticalBusinessUnionEntry
{
    public Guid ProportionalElectionUnionId { get; set; }

    public ProportionalElectionUnion ProportionalElectionUnion { get; set; } = null!; // set by ef

    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!; // set by ef
}
