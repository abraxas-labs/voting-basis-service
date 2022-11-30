// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Import;

public class MajorityElectionImport
{
    public MajorityElection Election { get; set; } = new();

    public IReadOnlyCollection<MajorityElectionCandidate> Candidates { get; set; } = Array.Empty<MajorityElectionCandidate>();
}
