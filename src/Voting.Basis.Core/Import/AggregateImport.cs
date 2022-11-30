// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Import;

internal class AggregateImport
{
    public List<MajorityElectionAggregate> MajorityElectionAggregates { get; } = new();

    public List<ProportionalElectionAggregate> ProportionalElectionAggregates { get; } = new();

    public List<VoteAggregate> VoteAggregates { get; } = new();
}
