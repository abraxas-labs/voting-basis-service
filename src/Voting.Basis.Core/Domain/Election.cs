// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class Election : PoliticalBusiness
{
    /// <summary>
    /// Gets or sets the number of mandates (in german: Anzahl Sitze).
    /// </summary>
    public int NumberOfMandates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the count of empty votes is calculated automatically by the system or
    /// if it must be entered manually by the user (as a kind of "double check").
    /// </summary>
    public bool AutomaticEmptyVoteCounting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the <see cref="AutomaticEmptyVoteCounting"/> setting.
    /// </summary>
    public bool EnforceEmptyVoteCountingForCountingCircles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the ResultEntry setting.
    /// </summary>
    public bool EnforceResultEntryForCountingCircles { get; set; }

    /// <summary>
    /// Gets or sets the domain of influence aggregation level for reports. It is "relative" to the domain of influence of this election,
    /// meaning that 0 equals the same level as the domain of influence. 1 would mean the child domain of influences and so on.
    /// </summary>
    public int ReportDomainOfInfluenceLevel { get; set; }

    /// <summary>
    /// Gets or sets the ballot bundle size. When entering results, multiple ballots are put together in a bundle (to identify them better etc.).
    /// </summary>
    public int BallotBundleSize { get; set; }

    /// <summary>
    /// Gets or sets the ballot bundle sample size. When ballot bundles are used when entering results, some of the ballots must be checked for their correctness.
    /// This configures the amount of ballots that must be sampled per ballot bundle.
    /// </summary>
    public int BallotBundleSampleSize { get; set; }

    /// <summary>
    /// Gets or sets the number generation strategy for ballots inside ballot bundles.
    /// </summary>
    public BallotNumberGeneration BallotNumberGeneration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether check digits must be used when entering candidate results.
    /// </summary>
    public bool CandidateCheckDigit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the CandidateCheckDigit setting.
    /// </summary>
    public bool EnforceCandidateCheckDigitForCountingCircles { get; set; }
}
