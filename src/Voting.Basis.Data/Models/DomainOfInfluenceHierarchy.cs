// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class DomainOfInfluenceHierarchy : BaseEntity
{
    public string TenantId { get; set; } = string.Empty;

    public Guid DomainOfInfluenceId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets ParentIds, ordered hierarchically ascending (the root is the last).
    /// </summary>
    public List<Guid> ParentIds { get; set; } = new List<Guid>();

    public List<Guid> ChildIds { get; set; } = new List<Guid>();

    public Guid RootId => ParentIds.Count > 0 ? ParentIds[^1] : DomainOfInfluenceId;
}
