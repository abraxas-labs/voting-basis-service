// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A ballot of a vote (in german: eine Vorlage der Abstimmung).
/// </summary>
public class Ballot
{
    /// <summary>
    /// Gets the position of this ballot inside the vote.
    /// </summary>
    public int Position { get; internal set; }

    public Guid Id { get; internal set; }

    public Guid VoteId { get; internal set; }

    public BallotType BallotType { get; internal set; }

    /// <summary>
    /// Gets the questions of this ballot. Only one question for standard ballots, multiple for variant ballots.
    /// </summary>
    public List<BallotQuestion> BallotQuestions { get; internal set; } = new();

    public bool HasTieBreakQuestions { get; internal set; }

    public List<TieBreakQuestion> TieBreakQuestions { get; internal set; } = new();
}
