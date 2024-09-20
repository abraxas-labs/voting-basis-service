// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models.Snapshots;

public class DomainOfInfluenceCountingCircleSnapshot : BaseEntity, ISnapshotEntity
{
    public Guid BasisId { get; set; }

    public Guid BasisDomainOfInfluenceId { get; set; }

    [NotMapped]
    public DomainOfInfluenceSnapshot DomainOfInfluence { get; set; } = null!;

    public Guid BasisCountingCircleId { get; set; }

    [NotMapped]
    public CountingCircleSnapshot CountingCircle { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the relation is inherited by a Child DomainOfInfluence or not.
    /// </summary>
    public bool Inherited { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether that the relation between doi and cc is deleted. This is only true when the relation
    /// was explicitly dissolved, and is still false when a CountingCircle or DomainOfInfluence in the relation is deleted.
    /// </summary>
    public bool Deleted { get; set; }
}
