// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data.ModelBuilders.Snapshots;

public class DomainOfInfluenceSnapshotModelBuilder :
    IEntityTypeConfiguration<DomainOfInfluenceSnapshot>,
    IEntityTypeConfiguration<DomainOfInfluenceCountingCircleSnapshot>
{
    public void Configure(EntityTypeBuilder<DomainOfInfluenceSnapshot> builder)
    {
        builder
            .HasIndex(x => new { x.BasisId });

        builder
            .HasIndex(x => new { x.ValidFrom });

        builder
            .OwnsOne(x => x.ContactPerson);
        builder
            .Navigation(x => x.ContactPerson).IsRequired();

        builder
            .OwnsOne(doi => doi.ReturnAddress);

        builder
            .OwnsOne(x => x.PrintData);

        builder
            .Property(x => x.ValidFrom)
            .HasUtcConversion();

        builder
            .Property(x => x.ValidTo)
            .HasUtcConversion();

        builder
            .Property(x => x.CreatedOn)
            .HasUtcConversion();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceCountingCircleSnapshot> builder)
    {
        builder
            .HasIndex(x => new { x.BasisId });

        builder
            .HasIndex(x => new { x.ValidFrom });

        builder
            .HasIndex(x => new { x.BasisCountingCircleId });

        builder
            .HasIndex(x => new { x.BasisDomainOfInfluenceId });

        builder
            .Property(x => x.CreatedOn)
            .HasUtcConversion();

        builder
            .Property(x => x.ValidFrom)
            .HasUtcConversion();

        builder
            .Property(x => x.ValidTo)
            .HasUtcConversion();

        builder
            .HasQueryFilter(x => !x.Deleted);
    }
}
