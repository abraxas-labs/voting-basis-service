// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Voting.Basis.Core.Extensions;

public static class AsyncEnumerableExtensions
{
    // Use custom extensions since we only need this method and System.Linq.Async collides with IQueryableExtensions on DbSet<T>.
    // This is also the reason why we use a custom namespace here instead of System.Linq or System.Collections.Generic.
    public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(
        this IAsyncEnumerable<TIn> enumerable,
        Func<TIn, TOut> mapper,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in enumerable.WithCancellation(ct))
        {
            yield return mapper(item);
        }
    }
}
