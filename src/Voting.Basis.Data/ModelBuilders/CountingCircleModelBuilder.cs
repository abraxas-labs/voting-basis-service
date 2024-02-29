// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class CountingCircleModelBuilder : IEntityTypeConfiguration<CountingCircle>,
    IEntityTypeConfiguration<CountingCirclesMerger>,
    IEntityTypeConfiguration<CountingCircleElectorate>
{
    public void Configure(EntityTypeBuilder<CountingCircle> builder)
    {
        builder
            .HasOne(cc => cc.ResponsibleAuthority!)
            .WithOne(a => a.CountingCircle!)
            .HasForeignKey<Authority>(e => e.CountingCircleId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(cc => cc.ContactPersonDuringEvent!)
            .WithOne(cp => cp.CountingCircleDuringEvent!)
            .HasForeignKey<CountingCircleContactPerson>(e => e.CountingCircleDuringEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cc => cc.ContactPersonAfterEvent!)
            .WithOne(cp => cp!.CountingCircleAfterEvent!)
            .HasForeignKey<CountingCircleContactPerson>(e => e.CountingCircleAfterEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(cc => cc.CreatedOn)
            .HasUtcConversion();

        builder
            .Property(cc => cc.ModifiedOn)
            .HasUtcConversion();

        builder
            .HasOne(cc => cc.MergeTarget!)
            .WithMany(c => c.MergedCountingCircles)
            .HasForeignKey(cc => cc.MergeTargetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasQueryFilter(cc => cc.State == CountingCircleState.Active);
    }

    public void Configure(EntityTypeBuilder<CountingCirclesMerger> builder)
    {
        builder
            .Property(x => x.ActiveFrom)
            .HasUtcConversion();

        builder
             .HasOne(c => c.NewCountingCircle)
             .WithOne(cc => cc.MergeOrigin!)
             .HasForeignKey<CountingCircle>(c => c.MergeOriginId)
             .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<CountingCircleElectorate> builder)
    {
        builder
            .HasOne(x => x.CountingCircle)
            .WithMany(x => x.Electorates)
            .HasForeignKey(x => x.CountingCircleId)
            .IsRequired();
    }
}
