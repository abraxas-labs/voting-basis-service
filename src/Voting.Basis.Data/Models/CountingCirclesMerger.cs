// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class CountingCirclesMerger : BaseEntity
{
    /// <summary>
    /// Gets or sets a value indicating whether the merge is already done.
    /// </summary>
    public bool Merged { get; set; }

    public DateTime ActiveFrom { get; set; }

    public Guid CopyFromCountingCircleId { get; set; }

    public ICollection<CountingCircle> MergedCountingCircles { get; set; } = new HashSet<CountingCircle>();

    public CountingCircle? NewCountingCircle { get; set; }
}
