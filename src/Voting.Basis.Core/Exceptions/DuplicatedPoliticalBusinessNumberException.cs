// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class DuplicatedPoliticalBusinessNumberException : Exception
{
    public DuplicatedPoliticalBusinessNumberException(string politicalBusinessNumber)
        : base($"The political business number {politicalBusinessNumber} is already taken")
    {
    }
}
