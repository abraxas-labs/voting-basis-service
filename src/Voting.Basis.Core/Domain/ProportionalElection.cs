// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ProportionalElection : Election
{
    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public ProportionalElectionReviewProcedure ReviewProcedure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the <see cref="ReviewProcedure"/> setting.
    /// </summary>
    public bool EnforceReviewProcedureForCountingCircles { get; set; }
}
