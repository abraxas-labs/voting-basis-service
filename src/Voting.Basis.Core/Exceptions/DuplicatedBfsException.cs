// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class DuplicatedBfsException : Exception
{
    public DuplicatedBfsException(string bfs)
        : base($"The bfs {bfs} is already taken")
    {
    }
}
