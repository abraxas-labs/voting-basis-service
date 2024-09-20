// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public abstract class BaseCountingCircle : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Bfs { get; set; } = string.Empty;

    public bool ContactPersonSameDuringEventAsAfter { get; set; }

    public CountingCircleState State { get; set; } = CountingCircleState.Active;

    public string Code { get; set; } = string.Empty;

    public int SortNumber { get; set; }

    public string NameForProtocol { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    public DomainOfInfluenceCanton Canton { get; set; }

    public bool EVoting { get; set; }

    public DateTime? EVotingActiveFrom { get; set; }
}
