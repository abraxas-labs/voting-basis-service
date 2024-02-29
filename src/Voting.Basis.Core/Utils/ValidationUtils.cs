// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Basis.Core.Exceptions;

namespace Voting.Basis.Core.Utils;

internal static class ValidationUtils
{
    /// <summary>
    /// Ensures that two objects are equal. If not, an exception is thrown.
    /// </summary>
    /// <param name="o1">The first object.</param>
    /// <param name="o2">The second object.</param>
    /// <typeparam name="T">The type of the objects.</typeparam>
    /// <exception cref="ModificationNotAllowedException">Throw if the two objects are not equal.</exception>
    internal static void EnsureNotModified<T>(T o1, T o2)
    {
        if (!EqualityComparer<T>.Default.Equals(o1, o2))
        {
            throw new ModificationNotAllowedException();
        }
    }
}
