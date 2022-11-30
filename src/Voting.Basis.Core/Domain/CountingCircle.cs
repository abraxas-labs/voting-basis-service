// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A counting circle (in german: Auszählungskreis) is the entity which counts the ballots (among other things).
/// </summary>
public class CountingCircle
{
    public Guid Id { get; set; }

    public Authority? ResponsibleAuthority { get; set; }

    /// <summary>
    /// Gets or sets the contact person which should be contacted during a "live" contest.
    /// </summary>
    public ContactPerson ContactPersonDuringEvent { get; set; } = new();

    /// <summary>
    /// Gets or sets the contact person which should be contacted after a contest has ended.
    /// </summary>
    public ContactPerson? ContactPersonAfterEvent { get; set; }

    public bool ContactPersonSameDuringEventAsAfter { get; set; }

    public int SortNumber { get; set; }

    public string NameForProtocol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain of influences that this counting circle belongs to.
    /// </summary>
    public ICollection<DomainOfInfluenceCountingCircle> DomainOfInfluences { get; set; } = new HashSet<DomainOfInfluenceCountingCircle>();

    public DateTime ModifiedOn { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    public ICollection<ContestCountingCircleOption> ContestOptions { get; set; } = new HashSet<ContestCountingCircleOption>();

    public CountingCirclesMerger? MergeTarget { get; set; }

    public Guid? MergeTargetId { get; set; }

    /// <summary>
    /// Gets or sets the origin of a counting circle merge, when multiple counting circles were merged into a new one.
    /// </summary>
    public CountingCirclesMerger? MergeOrigin { get; set; }

    public Guid? MergeOriginId { get; set; }

    /// <summary>
    /// Gets or sets the BFS (number from the "Bundesamt für Statistik) for this counting circle.
    /// </summary>
    public string Bfs { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the counting circle code, which is an arbitrary entered value by the user.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
