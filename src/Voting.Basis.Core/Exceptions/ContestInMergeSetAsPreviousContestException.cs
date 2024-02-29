// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

/// <summary>
/// Thrown when a contest should be merge into another one, but the contest is set as a previous contest in another contest.
/// </summary>
public class ContestInMergeSetAsPreviousContestException : Exception
{
    public ContestInMergeSetAsPreviousContestException()
        : base("contest in merge set as a previous contest of an other contest")
    {
    }
}
