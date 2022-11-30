﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public enum VoteReviewProcedure
{
    /// <summary>
    /// Vote review procedure is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The review procedure is performed electronically.
    /// </summary>
    Electronically,

    /// <summary>
    /// The review procedure is performed physically.
    /// </summary>
    Physically,
}
