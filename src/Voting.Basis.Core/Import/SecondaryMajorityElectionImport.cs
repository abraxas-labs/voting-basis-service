// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Domain;
using SecondaryMajorityElection = Voting.Basis.Core.Domain.SecondaryMajorityElection;

namespace Voting.Basis.Core.Import;

public class SecondaryMajorityElectionImport
{
    public SecondaryMajorityElection Election { get; set; } = new();

    public IReadOnlyCollection<MajorityElectionCandidate> Candidates { get; set; } = Array.Empty<MajorityElectionCandidate>();
}
