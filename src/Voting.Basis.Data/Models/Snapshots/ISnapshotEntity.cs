// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models.Snapshots;

public interface ISnapshotEntity
{
    Guid BasisId { get; set; }

    DateTime ValidFrom { get; set; }

    DateTime? ValidTo { get; set; }

    DateTime CreatedOn { get; set; }

    bool Deleted { get; set; }
}
