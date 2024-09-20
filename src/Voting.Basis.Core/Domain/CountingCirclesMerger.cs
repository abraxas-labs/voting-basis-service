// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class CountingCirclesMerger
{
    public Guid Id { get; set; }

    public bool Merged { get; set; }

    public DateTime ActiveFrom { get; set; }

    public Guid CopyFromCountingCircleId { get; set; }

    public ICollection<Guid> MergedCountingCircleIds { get; set; } = new HashSet<Guid>();

    public CountingCircle NewCountingCircle { get; set; } = new();
}
