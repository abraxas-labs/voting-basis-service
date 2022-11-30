// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class MajorityElectionUnion : PoliticalBusinessUnion
{
    public ICollection<MajorityElectionUnionEntry> MajorityElectionUnionEntries { get; set; }
        = new HashSet<MajorityElectionUnionEntry>();

    public override PoliticalBusinessUnionType Type => PoliticalBusinessUnionType.MajorityElection;
}
