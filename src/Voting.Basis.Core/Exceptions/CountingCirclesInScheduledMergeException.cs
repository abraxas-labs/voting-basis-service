// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class CountingCirclesInScheduledMergeException : Exception
{
    public CountingCirclesInScheduledMergeException()
        : base("at least one counting circle is in a scheduled merge")
    {
    }
}
