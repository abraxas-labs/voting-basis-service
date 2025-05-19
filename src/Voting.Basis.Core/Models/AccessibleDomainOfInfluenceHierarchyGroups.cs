// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Models;

public class AccessibleDomainOfInfluenceHierarchyGroups
{
    internal AccessibleDomainOfInfluenceHierarchyGroups(
        IReadOnlySet<Guid> tenantDoiIds,
        IReadOnlySet<Guid> parentDoiIds,
        IReadOnlySet<Guid> childDoiIds)
    {
        TenantDoiIds = tenantDoiIds;
        ParentDoiIds = parentDoiIds;
        ChildDoiIds = childDoiIds;

        var tenantAndChildDoiIds = new HashSet<Guid>(TenantDoiIds);
        tenantAndChildDoiIds.UnionWith(ChildDoiIds);
        TenantAndChildDoiIds = tenantAndChildDoiIds;

        var tenantAndParentDoiIds = new HashSet<Guid>(TenantDoiIds);
        tenantAndParentDoiIds.UnionWith(ParentDoiIds);
        TenantAndParentDoiIds = tenantAndParentDoiIds;

        var accessibleDoiIds = new HashSet<Guid>(TenantAndParentDoiIds);
        accessibleDoiIds.UnionWith(ChildDoiIds);
        AccessibleDoiIds = accessibleDoiIds;
    }

    /// <summary>
    /// Gets the domain of influence IDs that "belong" to the specified tenant (tenant is the owner of the DOI).
    /// </summary>
    public IReadOnlySet<Guid> TenantDoiIds { get; }

    /// <summary>
    /// Gets the domain of influence IDs that are parents of <see cref="TenantDoiIds"/> (higher in the DOI hierarchy).
    /// </summary>
    public IReadOnlySet<Guid> ParentDoiIds { get; }

    /// <summary>
    /// Gets the domain of influence IDs that are children of <see cref="TenantDoiIds"/> (lower in the DOI hierarchy).
    /// </summary>
    public IReadOnlySet<Guid> ChildDoiIds { get; }

    public IReadOnlySet<Guid> TenantAndChildDoiIds { get; }

    public IReadOnlySet<Guid> TenantAndParentDoiIds { get; }

    /// <summary>
    /// Gets all domain of influence IDs that are in the DOI hierarchy of the specified tenant.
    /// Includes domain of influences that "belong" to the tenant plus DOIs higher or lower in the hierarchy.
    /// </summary>
    public IReadOnlySet<Guid> AccessibleDoiIds { get; }
}
