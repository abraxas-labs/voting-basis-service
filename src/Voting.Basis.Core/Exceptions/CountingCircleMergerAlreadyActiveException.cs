// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class CountingCircleMergerAlreadyActiveException : Exception
{
    internal CountingCircleMergerAlreadyActiveException()
        : base("The merger is already active")
    {
    }
}
