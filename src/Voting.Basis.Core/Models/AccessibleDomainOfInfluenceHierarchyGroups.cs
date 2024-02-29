// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Basis.Core.Models;

public class AccessibleDomainOfInfluenceHierarchyGroups
{
    /// <summary>
    /// Gets or sets the domain of influence IDs that "belong" to the specified tenant (tenant is the owner of the DOI).
    /// </summary>
    public List<Guid> TenantDoiIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the domain of influence IDs that are parents of <see cref="TenantDoiIds"/> (higher in the DOI hierarchy).
    /// </summary>
    public List<Guid> ParentDoiIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the domain of influence IDs that are children of <see cref="TenantDoiIds"/> (lower in the DOI hierarchy).
    /// </summary>
    public List<Guid> ChildDoiIds { get; set; } = new();

    public List<Guid> TenantAndChildDoiIds => TenantDoiIds.Concat(ChildDoiIds).ToList();

    public List<Guid> TenantAndParentDoiIds => TenantDoiIds.Concat(ParentDoiIds).ToList();

    /// <summary>
    /// Gets all domain of influence IDs that are in the DOI hierarchy of the specified tenant.
    /// Includes domain of influences that "belong" to the tenant plus DOIs higher or lower in the hierarchy.
    /// </summary>
    public List<Guid> AccessibleDoiIds => TenantAndParentDoiIds.Concat(ChildDoiIds).ToList();
}
