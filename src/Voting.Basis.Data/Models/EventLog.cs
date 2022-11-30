// (c) Copyright 2022 by Abraxas Informatik AG
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

    public Guid? PoliticalBusinessId { get; set; }

    public Guid? PoliticalBusinessUnionId { get; set; }
}
