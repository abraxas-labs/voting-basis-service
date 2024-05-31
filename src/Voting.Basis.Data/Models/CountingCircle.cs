// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data.Models;

public class CountingCircle : BaseCountingCircle, IHasSnapshotEntity<CountingCircleSnapshot>
{
    public Authority ResponsibleAuthority { get; set; } = new Authority();

    public CountingCircleContactPerson ContactPersonDuringEvent { get; set; } = new CountingCircleContactPerson();

    public CountingCircleContactPerson? ContactPersonAfterEvent { get; set; }

    public ICollection<DomainOfInfluenceCountingCircle> DomainOfInfluences { get; set; } = new HashSet<DomainOfInfluenceCountingCircle>();

    public DateTime ModifiedOn { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    public ICollection<CountingCircleElectorate> Electorates { get; set; } = new HashSet<CountingCircleElectorate>();

    public CountingCirclesMerger? MergeTarget { get; set; }

    public Guid? MergeTargetId { get; set; }

    public CountingCirclesMerger? MergeOrigin { get; set; }

    public Guid? MergeOriginId { get; set; }
}
