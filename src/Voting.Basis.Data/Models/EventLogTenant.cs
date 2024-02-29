// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class EventLogTenant : BaseEntity
{
    public string TenantId { get; set; } = string.Empty;

    public string TenantName { get; set; } = string.Empty;

    public ICollection<EventLog> EventLogs { get; set; } = new HashSet<EventLog>();
}
