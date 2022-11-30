// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class CountingCircleInScheduledMergeException : Exception
{
    public CountingCircleInScheduledMergeException()
        : base("counting circle is in a scheduled merge")
    {
    }
}
