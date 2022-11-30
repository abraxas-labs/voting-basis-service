// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A tie break question of a variant ballot (in german: Stichfrage).
/// </summary>
public class TieBreakQuestion
{
    /// <summary>
    /// Gets or sets the question number of this tie break question.
    /// </summary>
    public int Number { get; set; }

    public Dictionary<string, string> Question { get; set; } = new();

    /// <summary>
    /// Gets or sets the question number 1. This is a reference to the first <see cref="BallotQuestion"/> question number.
    /// </summary>
    public int Question1Number { get; set; }

    /// <summary>
    /// Gets or sets the question number 2. This is a reference to the second <see cref="BallotQuestion"/> number.
    /// </summary>
    public int Question2Number { get; set; }

    public Ballot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }
}
