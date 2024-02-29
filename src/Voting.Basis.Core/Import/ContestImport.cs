// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Import;

public class ContestImport
{
    public Contest Contest { get; private set; } = null!;

    public List<MajorityElectionImport> MajorityElections { get; private set; } = new();

    public List<ProportionalElectionImport> ProportionalElections { get; private set; } = new();

    public List<VoteImport> Votes { get; private set; } = new();
}
