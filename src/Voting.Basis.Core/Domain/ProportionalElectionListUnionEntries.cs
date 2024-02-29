// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class ProportionalElectionListUnionEntries
{
    public Guid ProportionalElectionListUnionId { get; set; }

    public List<Guid> ProportionalElectionListIds { get; set; } = new();
}
