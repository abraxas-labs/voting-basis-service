// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voting.Basis.Data.Models.Snapshots;

public class DomainOfInfluenceSnapshot : BaseDomainOfInfluence, ISnapshotEntity
{
    public Guid BasisId { get; set; }

    public Guid? BasisParentId { get; set; }

    [NotMapped]
    public DomainOfInfluenceSnapshot? Parent { get; set; }

    [NotMapped]
    public ICollection<DomainOfInfluenceCountingCircleSnapshot> CountingCircles { get; set; }
        = new HashSet<DomainOfInfluenceCountingCircleSnapshot>();

    [NotMapped]
    public ICollection<DomainOfInfluenceSnapshot> Children { get; set; }
        = new HashSet<DomainOfInfluenceSnapshot>();

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool Deleted { get; set; }
}
