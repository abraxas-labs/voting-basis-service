// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data.ModelBuilders;

public class CountingCircleSnapshotModelBuilder : IEntityTypeConfiguration<CountingCircleSnapshot>
{
    public void Configure(EntityTypeBuilder<CountingCircleSnapshot> builder)
    {
        builder
            .HasIndex(x => new { x.BasisId });

        builder
            .HasIndex(x => new { x.ValidFrom });

        builder
            .HasOne(x => x.ResponsibleAuthority!)
            .WithOne(x => x.CountingCircle!)
            .HasForeignKey<AuthoritySnapshot>(x => x.CountingCircleId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(x => x.ContactPersonDuringEvent!)
            .WithOne(x => x.CountingCircleDuringEvent!)
            .HasForeignKey<CountingCircleContactPersonSnapshot>(x => x.CountingCircleDuringEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.ContactPersonAfterEvent!)
            .WithOne(x => x!.CountingCircleAfterEvent!)
            .HasForeignKey<CountingCircleContactPersonSnapshot>(x => x.CountingCircleAfterEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(x => x.ValidFrom)
            .HasUtcConversion();

        builder
            .Property(x => x.ValidTo)
            .HasUtcConversion();

        builder
            .Property(x => x.CreatedOn)
            .HasUtcConversion();

        builder
            .HasQueryFilter(cc => cc.State != CountingCircleState.Inactive);

        builder
            .Property(d => d.EVotingActiveFrom)
            .HasDateType()
            .HasUtcConversion();
    }
}
