// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class Vote : PoliticalBusiness
{
    /// <summary>
    /// Gets or sets the domain of influence aggregation level for reports. It is "relative" to the domain of influence of this vote,
    /// meaning that 0 equals the same level as the domain of influence. 1 would mean the child domain of influences and so on.
    /// </summary>
    public int ReportDomainOfInfluenceLevel { get; set; }

    public VoteResultAlgorithm ResultAlgorithm { get; set; }

    public VoteResultEntry ResultEntry { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the <see cref="ResultEntry"/> setting.
    /// </summary>
    public bool EnforceResultEntryForCountingCircles { get; set; }

    public List<Ballot> Ballots { get; set; } = new();

    /// <summary>
    /// Gets or sets the percentage of ballots inside a ballot bundle that must be sampled for correctness.
    /// Note that this is the integer value of a percentage, ie. a value of 77 would mean 77%.
    /// </summary>
    public int BallotBundleSampleSizePercent { get; set; }

    public VoteReviewProcedure ReviewProcedure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the <see cref="ReviewProcedure"/> setting.
    /// </summary>
    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public string InternalDescription { get; set; } = string.Empty;
}
