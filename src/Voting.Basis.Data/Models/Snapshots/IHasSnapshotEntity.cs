// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models.Snapshots;

public interface IHasSnapshotEntity<T>
{
    DateTime CreatedOn { get; set; }

    DateTime ModifiedOn { get; set; }
}
