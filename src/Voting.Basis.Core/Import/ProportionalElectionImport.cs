// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Import;

public class ProportionalElectionImport
{
    public ProportionalElection Election { get; set; } = new();

    public IReadOnlyCollection<ProportionalElectionListImport> Lists { get; set; } = Array.Empty<ProportionalElectionListImport>();

    public IReadOnlyCollection<ProportionalElectionListUnion> ListUnions { get; set; } = Array.Empty<ProportionalElectionListUnion>();
}
