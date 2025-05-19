// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class EventLog : BaseEntity
{
    public string EventContent { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public Guid EventUserId { get; set; }

    public EventLogUser? EventUser { get; set; }

    public Guid EventTenantId { get; set; }

    public EventLogTenant? EventTenant { get; set; }

    public Guid? CountingCircleId { get; set; }

    public Guid? DomainOfInfluenceId { get; set; }

    public Guid? ContestId { get; set; }

    /// <summary>
    /// Gets or sets the id of the aggregate this event was applied to.
    /// This is only set for events processed by the processor after this field was introduced.
    /// E.g. if the event was fired after the introduction of this field,
    /// or the events were replayed since.
    /// </summary>
    public Guid? AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the id of the entity this event affected.
    /// This is only set for events processed by the processor after this field was introduced.
    /// E.g. if the event was fired after the introduction of this field,
    /// or the events were replayed since.
    /// </summary>
    public Guid? EntityId { get; set; }

    public Guid? PoliticalBusinessId { get; set; }

    public Guid? PoliticalBusinessUnionId { get; set; }

    public Guid? PoliticalAssemblyId { get; set; }
}
