// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ContestWithExistingPoliticalBusinessesException : Exception
{
    public ContestWithExistingPoliticalBusinessesException()
        : base("Contest with existing political businesses cannot be deleted")
    {
    }

    public ContestWithExistingPoliticalBusinessesException(string? message)
        : base(message)
    {
    }

    public ContestWithExistingPoliticalBusinessesException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
