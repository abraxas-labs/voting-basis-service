// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Domain;

public class ContestCountingCircleOption
{
    public Guid CountingCircleId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether E-Voting is allowed in this counting circle.
    /// </summary>
    public bool EVoting { get; private set; }
}
