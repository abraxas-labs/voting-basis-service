// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class ProportionalElectionUnion : PoliticalBusinessUnion
{
    public ICollection<ProportionalElectionUnionEntry> ProportionalElectionUnionEntries { get; set; }
        = new HashSet<ProportionalElectionUnionEntry>();

    public ICollection<ProportionalElectionUnionList> ProportionalElectionUnionLists { get; set; }
        = new HashSet<ProportionalElectionUnionList>();

    public override PoliticalBusinessUnionType Type => PoliticalBusinessUnionType.ProportionalElection;
}
