// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// An election group (in german: Wahlgruppe). This exists when a majority election has secondary majority elections.
/// </summary>
public class ElectionGroup
{
    public Guid Id { get; internal set; }

    public string Description { get; internal set; } = string.Empty;

    public int Position { get; private set; }

    public Guid PrimaryMajorityElectionId { get; internal set; }

    public int Number { get; set; }
}
