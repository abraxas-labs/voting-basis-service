// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

/// <summary>
/// Thrown when an entity already exists, but shouldn't.
/// </summary>
public class AlreadyExistsException : Exception
{
    public AlreadyExistsException(string msg)
        : base(msg)
    {
    }
}
