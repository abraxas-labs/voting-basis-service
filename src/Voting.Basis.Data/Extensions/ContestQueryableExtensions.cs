// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.Extensions;

public static class ContestQueryableExtensions
{
    /// <summary>
    /// Sorts the contests ascending by date if it contains an upcoming state, descending otherwise.
    /// </summary>
    /// <param name="query">Contest queryable.</param>
    /// <param name="states">Contest states.</param>
    /// <returns>Sorted contest queryable.</returns>
    public static IQueryable<Contest> Order(this IQueryable<Contest> query, IReadOnlyCollection<ContestState> states)
    {
        return states.Contains(ContestState.TestingPhase) || states.Contains(ContestState.Active)
            ? query.OrderBy(x => x.Date)
            : query.OrderByDescending(x => x.Date);
    }
}
