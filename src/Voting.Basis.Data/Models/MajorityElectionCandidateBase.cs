// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public abstract class MajorityElectionCandidateBase : ElectionCandidate
{
    public Dictionary<string, string> Party { get; set; } = new Dictionary<string, string>();
}
