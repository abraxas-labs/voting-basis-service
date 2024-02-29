// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A list union (in german: Listenverbindung).
/// </summary>
public class ProportionalElectionListUnion
{
    public ProportionalElectionListUnion()
    {
        Description = new Dictionary<string, string>();
        ProportionalElectionListIds = new List<Guid>();
    }

    public Guid Id { get; internal set; }

    public Dictionary<string, string> Description { get; internal set; }

    public int Position { get; internal set; }

    /// <summary>
    /// Gets the root list union ID inside sub list unions.
    /// </summary>
    public Guid? ProportionalElectionRootListUnionId { get; internal set; }

    public List<Guid> ProportionalElectionListIds { get; internal set; }

    public Guid? ProportionalElectionMainListId { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this is a sub list union (in german: Unterlistenverbindung).
    /// This is a "list union inside another list union".
    /// </summary>
    public bool IsSubListUnion => ProportionalElectionRootListUnionId.HasValue;

    public Guid ProportionalElectionId { get; internal set; }
}
