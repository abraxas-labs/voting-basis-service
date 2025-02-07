// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class ProportionalElectionModelBuilder :
    IEntityTypeConfiguration<ProportionalElection>,
    IEntityTypeConfiguration<ProportionalElectionList>,
    IEntityTypeConfiguration<ProportionalElectionUnionListEntry>,
    IEntityTypeConfiguration<ProportionalElectionUnionList>,
    IEntityTypeConfiguration<ProportionalElectionUnionEntry>,
    IEntityTypeConfiguration<ProportionalElectionUnion>,
    IEntityTypeConfiguration<ProportionalElectionCandidate>,
    IEntityTypeConfiguration<ProportionalElectionListUnionEntry>,
    IEntityTypeConfiguration<ProportionalElectionListUnion>
{
    public void Configure(EntityTypeBuilder<ProportionalElection> builder)
    {
        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di!.ProportionalElections)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.ProportionalElections)
            .HasForeignKey(v => v.ContestId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.ProportionalElectionLists)
            .WithOne(l => l.ProportionalElection)
            .HasForeignKey(l => l.ProportionalElectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.ProportionalElectionListUnions)
            .WithOne(lu => lu.ProportionalElection)
            .HasForeignKey(lu => lu.ProportionalElectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(x => x.OfficialDescription)
            .HasJsonConversion();

        builder
            .Property(x => x.ShortDescription)
            .HasJsonConversion();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionList> builder)
    {
        builder
            .HasMany(p => p.ProportionalElectionCandidates)
            .WithOne(l => l.ProportionalElectionList)
            .HasForeignKey(l => l.ProportionalElectionListId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(x => x.Description)
            .HasJsonConversion();

        builder
            .Property(x => x.ShortDescription)
            .HasJsonConversion();

        // OnDelete.SetNull is required for domain of influence delete events, because it hard deletes the party.
        // If a party is separately removed, it soft deletes the party.
        builder
            .HasOne(x => x.Party)
            .WithMany(x => x.ProportionalElectionLists)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionListEntry> builder)
    {
        builder
            .HasOne(ple => ple.ProportionalElectionUnionList)
            .WithMany(pul => pul.ProportionalElectionUnionListEntries)
            .HasForeignKey(ple => ple.ProportionalElectionUnionListId)
            .IsRequired();

        builder
            .HasOne(ple => ple.ProportionalElectionList)
            .WithMany(pl => pl.ProportionalElectionUnionListEntries)
            .HasForeignKey(ple => ple.ProportionalElectionListId)
            .IsRequired();

        builder
            .HasIndex(ple => new { ple.ProportionalElectionListId, ple.ProportionalElectionUnionListId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionList> builder)
    {
        builder
            .HasOne(pul => pul.ProportionalElectionUnion)
            .WithMany(pu => pu.ProportionalElectionUnionLists)
            .HasForeignKey(pul => pul.ProportionalElectionUnionId)
            .IsRequired();

        builder
            .Property(x => x.ShortDescription)
            .HasJsonConversion();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionEntry> builder)
    {
        builder
            .HasOne(pe => pe.ProportionalElectionUnion)
            .WithMany(pu => pu.ProportionalElectionUnionEntries)
            .HasForeignKey(pe => pe.ProportionalElectionUnionId)
            .IsRequired();

        builder
            .HasOne(pe => pe.ProportionalElection)
            .WithMany(pu => pu.ProportionalElectionUnionEntries)
            .HasForeignKey(pe => pe.ProportionalElectionId)
            .IsRequired();

        builder
            .HasIndex(pe => new { pe.ProportionalElectionId, pe.ProportionalElectionUnionId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnion> builder)
    {
        builder
            .HasOne(pu => pu.Contest)
            .WithMany(c => c.ProportionalElectionUnions)
            .HasForeignKey(pu => pu.ContestId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidate> builder)
    {
        builder
            .Property(d => d.DateOfBirth)
            .HasDateType()
            .HasUtcConversion()
            .IsRequired();

        builder
            .Property(x => x.Occupation)
            .HasJsonConversion();

        builder
            .Property(x => x.OccupationTitle)
            .HasJsonConversion();

        // OnDelete.SetNull is required for domain of influence delete events, because it hard deletes the party.
        // If a party is separately removed, it soft deletes the party.
        builder
            .HasOne(x => x.Party)
            .WithMany(x => x.ProportionalElectionCandidates)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListUnionEntry> builder)
    {
        builder
            .HasKey(e => new { e.ProportionalElectionListId, e.ProportionalElectionListUnionId });

        builder
            .HasOne(e => e.ProportionalElectionList)
            .WithMany(l => l.ProportionalElectionListUnionEntries)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(e => e.ProportionalElectionListUnion)
            .WithMany(lu => lu.ProportionalElectionListUnionEntries)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListUnion> builder)
    {
        builder
            .HasOne(lu => lu.ProportionalElectionRootListUnion)
            .WithMany(lu => lu!.ProportionalElectionSubListUnions)
            .HasForeignKey(lu => lu.ProportionalElectionRootListUnionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(lu => lu.ProportionalElectionMainList)
            .WithMany(l => l!.ProportionalElectionMainListUnions)
            .HasForeignKey(lu => lu.ProportionalElectionMainListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(x => x.Description)
            .HasJsonConversion();
    }
}
