// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voting.Basis.Data.Models.Snapshots;

public class CountingCircleSnapshot : BaseCountingCircle, ISnapshotEntity
{
    public Guid BasisId { get; set; }

    public AuthoritySnapshot ResponsibleAuthority { get; set; } = new AuthoritySnapshot();

    public CountingCircleContactPersonSnapshot ContactPersonDuringEvent { get; set; } = new CountingCircleContactPersonSnapshot();

    public CountingCircleContactPersonSnapshot? ContactPersonAfterEvent { get; set; }

    [NotMapped]
    public ICollection<DomainOfInfluenceCountingCircleSnapshot> DomainOfInfluences { get; set; }
        = new HashSet<DomainOfInfluenceCountingCircleSnapshot>();

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool Deleted { get; set; }
}
