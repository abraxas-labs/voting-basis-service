// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System.Collections.Generic;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Import;

public class ProportionalElectionImportCandidate : ProportionalElectionCandidate
{
    public Dictionary<string, string>? SourcePartyShort { get; set; }

    public Dictionary<string, string>? SourceParty { get; set; }
}
