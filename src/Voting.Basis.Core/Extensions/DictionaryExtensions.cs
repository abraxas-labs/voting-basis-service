// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Core.Extensions;

public static class DictionaryExtensions
{
    /// <summary>
    /// Checks whether two dictionary contain the exact same key-value pairs.
    /// </summary>
    /// <param name="dict1">The first dictionary.</param>
    /// <param name="dict2">The second dictionary.</param>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <returns>True if both dictionaries contain the same key-value pairs or if both dictionary are null.</returns>
    public static bool KeysAndValuesEqual<TKey, TValue>(this IDictionary<TKey, TValue>? dict1, IDictionary<TKey, TValue>? dict2)
        where TKey : notnull
    {
        if (dict1 == dict2)
        {
            return true;
        }

        // If both are null, that would have been caught by the object equality check above
        if (dict1 == null || dict2 == null || dict1.Count != dict2.Count)
        {
            return false;
        }

        ICollection<KeyValuePair<TKey, TValue>> dict2Collection = dict2;
        foreach (var entry in dict1)
        {
            if (!dict2Collection.Contains(entry))
            {
                return false;
            }
        }

        return true;
    }
}
