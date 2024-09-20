// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

/// <summary>
/// Thrown when a contest is (currently) immutable, but it was tried to modify it.
/// </summary>
public class ContestLockedException : Exception
{
    public ContestLockedException()
    : base("Contest is past locked or archived")
    {
    }
}
