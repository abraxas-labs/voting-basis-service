// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public enum BallotType
{
    /// <summary>
    /// Ballot type is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Ballot type standard (only one question).
    /// </summary>
    StandardBallot,

    /// <summary>
    /// Ballot type variants (multiple questions).
    /// </summary>
    VariantsBallot,
}
