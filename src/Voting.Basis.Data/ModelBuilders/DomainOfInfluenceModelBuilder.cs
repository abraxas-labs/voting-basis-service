// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class DomainOfInfluenceModelBuilder :
    IEntityTypeConfiguration<DomainOfInfluence>,
    IEntityTypeConfiguration<DomainOfInfluenceCountingCircle>,
    IEntityTypeConfiguration<DomainOfInfluencePermissionEntry>,
    IEntityTypeConfiguration<DomainOfInfluenceHierarchy>,
    IEntityTypeConfiguration<DomainOfInfluenceParty>
{
    public void Configure(EntityTypeBuilder<DomainOfInfluence> builder)
    {
        builder
            .HasOne(di => di.Parent!)
            .WithMany(di => di!.Children)
            .HasForeignKey(di => di.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(cc => cc.CreatedOn)
            .HasUtcConversion();

        builder
            .Property(cc => cc.ModifiedOn)
            .HasUtcConversion();

        builder.OwnsOne(doi => doi.ContactPerson);
        builder.Navigation(doi => doi.ContactPerson).IsRequired();

        builder.OwnsOne(doi => doi.CantonDefaults, b =>
        {
            b.Property(x => x.ProportionalElectionMandateAlgorithms)
                .HasPostgresEnumListToIntListConversion();

            b.Property(x => x.EnabledPoliticalBusinessUnionTypes)
                .HasPostgresEnumListToIntListConversion();
        });
        builder.Navigation(doi => doi.CantonDefaults).IsRequired();

        builder.OwnsOne(doi => doi.ReturnAddress);

        builder.OwnsOne(doi => doi.PrintData);

        builder.OwnsOne(doi => doi.SwissPostData);
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceCountingCircle> builder)
    {
        builder
            .HasOne(dicc => dicc.CountingCircle)
            .WithMany(cc => cc!.DomainOfInfluences)
            .HasForeignKey(dicc => dicc.CountingCircleId)
            .IsRequired();

        builder
            .HasOne(dicc => dicc.DomainOfInfluence)
            .WithMany(di => di!.CountingCircles)
            .HasForeignKey(dicc => dicc.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CountingCircleId, x.DomainOfInfluenceId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluencePermissionEntry> builder)
    {
        builder
            .HasIndex(x => new { x.TenantId, x.DomainOfInfluenceId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceHierarchy> builder)
    {
        builder
            .HasIndex(x => x.DomainOfInfluenceId)
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceParty> builder)
    {
        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithMany(x => x.Parties)
            .IsRequired();

        builder
            .Property(x => x.Name)
            .HasJsonConversion();

        builder
            .Property(x => x.ShortDescription)
            .HasJsonConversion();

        builder
            .HasQueryFilter(x => !x.Deleted);
    }
}
