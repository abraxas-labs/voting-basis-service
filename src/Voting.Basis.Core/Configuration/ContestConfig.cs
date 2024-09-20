// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Configuration;

public class ContestConfig
{
    /// <summary>
    /// Gets or sets the contest warn period. When a contest is created, check if any other contest exists in this period.
    /// If a contests exists, a warning is emitted.
    /// </summary>
    public TimeSpan ContestCreationWarnPeriod { get; set; }

    /// <summary>
    /// Gets or sets the maximal timespan in which the testing phase must end before the contest.
    /// </summary>
    public TimeSpan EndOfTestingPhaseMaxTimespanBeforeContest { get; set; }
}
