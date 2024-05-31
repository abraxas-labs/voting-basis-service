// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class NonUniqueCandidateNumberException : Exception
{
    public NonUniqueCandidateNumberException()
        : base("This candidate number is already taken")
    {
    }

    public NonUniqueCandidateNumberException(string? message)
        : base(message)
    {
    }

    public NonUniqueCandidateNumberException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
