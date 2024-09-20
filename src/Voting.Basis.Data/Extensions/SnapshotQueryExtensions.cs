// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Voting.Basis.Data.Models.Snapshots;

namespace Microsoft.EntityFrameworkCore;

public static class SnapshotQueryExtensions
{
    public static IQueryable<TSnapshotEntity> ValidOn<TSnapshotEntity>(this IQueryable<TSnapshotEntity> query, DateTime referenceDateTime)
        where TSnapshotEntity : ISnapshotEntity
    {
        return query.Where(x => x.ValidFrom <= referenceDateTime && (x.ValidTo == null || x.ValidTo.Value > referenceDateTime));
    }
}
