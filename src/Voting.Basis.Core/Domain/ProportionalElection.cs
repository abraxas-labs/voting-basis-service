// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ProportionalElection : Election
{
    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether check digits must be used when entering candidate results.
    /// </summary>
    public bool CandidateCheckDigit { get; set; }

    public ProportionalElectionReviewProcedure ReviewProcedure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the <see cref="ReviewProcedure"/> setting.
    /// </summary>
    public bool EnforceReviewProcedureForCountingCircles { get; set; }
}
