// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Import;

public class ProportionalElectionListImport
{
    public ProportionalElectionList List { get; set; } = new();

    public IReadOnlyCollection<ProportionalElectionImportCandidate> Candidates { get; set; } = Array.Empty<ProportionalElectionImportCandidate>();
}
