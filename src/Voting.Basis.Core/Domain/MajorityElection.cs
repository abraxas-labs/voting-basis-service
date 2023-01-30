// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A majority election (in german: Majorzwahl).
/// </summary>
public class MajorityElection : Election
{
    public MajorityElectionMandateAlgorithm MandateAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether check digits must be used when entering candidate results.
    /// </summary>
    public bool CandidateCheckDigit { get; set; }

    public MajorityElectionResultEntry ResultEntry { get; set; }

    public List<MajorityElectionCandidate> Candidates { get; private set; } = new();

    public List<MajorityElectionBallotGroup> BallotGroups { get; private set; } = new();

    public List<SecondaryMajorityElection> SecondaryMajorityElections { get; private set; } = new();

    public MajorityElectionReviewProcedure ReviewProcedure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can override the <see cref="ReviewProcedure"/> setting.
    /// </summary>
    public bool EnforceReviewProcedureForCountingCircles { get; set; }
}
