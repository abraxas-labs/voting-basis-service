// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.EventSignature.Exceptions;

public class ContestCacheException : Exception
{
    public ContestCacheException(string? message)
    : base(message)
    {
    }
}
