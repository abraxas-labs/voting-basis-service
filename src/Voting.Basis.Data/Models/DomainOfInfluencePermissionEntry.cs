// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class DomainOfInfluencePermissionEntry : BaseEntity
{
    public string TenantId { get; set; } = string.Empty;

    public Guid DomainOfInfluenceId { get; set; } = Guid.Empty;

    public List<Guid> CountingCircleIds { get; set; } = new List<Guid>();

    /// <summary>
    /// Gets or sets a value indicating whether the TenantId is only assigned on a child level or a counting circle.
    /// </summary>
    public bool IsParent { get; set; } = true;
}
