// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace System.Linq;

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> Flatten<TSource>(
        this IEnumerable<TSource> enumerable,
        Func<TSource, IEnumerable<TSource>> childrenSelector)
    {
        return enumerable.SelectMany(c => childrenSelector(c).Flatten(childrenSelector)).Concat(enumerable);
    }

    public static int MaxOrDefault<TSource>(this IEnumerable<TSource> enumerable, Func<TSource, int> propSelector)
    {
        return enumerable.Any()
            ? enumerable.Max(propSelector)
            : 0;
    }

    /// <summary>
    /// Compares an existing collection to an updated list of items and generates a diff of that.
    /// Then applies that diff to the existing collection.
    /// </summary>
    /// <param name="items">The items to update.</param>
    /// <param name="updated">The updated list to use as a comparison.</param>
    /// <param name="identitySelector">The identity selector.</param>
    /// <param name="factory">The factory function to apply to added items.</param>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <typeparam name="TIdentity">The type of the item identity.</typeparam>
    public static void Update<T, TIdentity>(
        this ICollection<T> items,
        IEnumerable<T> updated,
        Func<T, TIdentity> identitySelector,
        Func<T, T>? factory = null)
        where T : notnull
        where TIdentity : notnull
    {
        factory ??= x => x;
        var diff = items.BuildDiff(updated, identitySelector);

        foreach (var removed in diff.Removed)
        {
            items.Remove(removed);
        }

        foreach (var added in diff.Added)
        {
            items.Add(factory(added));
        }
    }
}
