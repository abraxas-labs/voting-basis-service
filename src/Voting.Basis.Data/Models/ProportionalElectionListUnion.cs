// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class ProportionalElectionListUnion : BaseEntity
{
    public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();

    public int Position { get; set; }

    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public Guid? ProportionalElectionRootListUnionId { get; set; }

    public ProportionalElectionListUnion? ProportionalElectionRootListUnion { get; set; }

    public Guid? ProportionalElectionMainListId { get; set; }

    public ProportionalElectionList? ProportionalElectionMainList { get; set; }

    public ICollection<ProportionalElectionListUnion> ProportionalElectionSubListUnions { get; set; } = new HashSet<ProportionalElectionListUnion>();

    public ICollection<ProportionalElectionListUnionEntry> ProportionalElectionListUnionEntries { get; set; } = new HashSet<ProportionalElectionListUnionEntry>();

    public bool IsSubListUnion => ProportionalElectionRootListUnionId.HasValue;
}
