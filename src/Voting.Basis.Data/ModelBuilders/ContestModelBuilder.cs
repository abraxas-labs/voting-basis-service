// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class ContestModelBuilder :
    IEntityTypeConfiguration<Contest>,
    IEntityTypeConfiguration<PreconfiguredContestDate>,
    IEntityTypeConfiguration<ContestCountingCircleOption>
{
    public void Configure(EntityTypeBuilder<Contest> builder)
    {
        builder
            .Property(d => d.Date)
            .HasDateType()
            .HasUtcConversion();

        builder
            .Property(d => d.EndOfTestingPhase)
            .HasUtcConversion();

        builder
            .Property(d => d.EVotingFrom)
            .HasUtcConversion();

        builder
            .Property(d => d.EVotingTo)
            .HasUtcConversion();

        builder
            .HasOne(c => c.DomainOfInfluence)
            .WithMany(doi => doi.Contests)
            .HasForeignKey(c => c.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasOne(c => c.PreviousContest)
            .WithMany(c => c.PreviousContestOwners)
            .HasForeignKey(c => c.PreviousContestId);

        builder
            .Property(x => x.ArchivePer)
            .HasUtcConversion();

        builder
            .Property(x => x.PastLockPer)
            .HasUtcConversion();

        // contest reader queries by state and doiId if not an admin
        // if the contest list is requested
        // (expected to happen often since it is the entry point of the app)
        builder.HasIndex(x => new { x.State, x.DomainOfInfluenceId });

        builder
            .Property(x => x.Description)
            .HasJsonConversion();
    }

    public void Configure(EntityTypeBuilder<PreconfiguredContestDate> builder)
    {
        builder
            .Property(d => d.Id)
            .HasDateType()
            .HasUtcConversion();
    }

    public void Configure(EntityTypeBuilder<ContestCountingCircleOption> builder)
    {
        builder
            .HasOne(x => x.Contest!)
            .WithMany(x => x.CountingCircleOptions)
            .HasForeignKey(x => x.ContestId)
            .IsRequired();

        builder
            .HasOne(x => x.CountingCircle!)
            .WithMany(x => x.ContestOptions)
            .HasForeignKey(x => x.CountingCircleId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CountingCircleId, x.ContestId })
            .IsUnique();
    }
}
