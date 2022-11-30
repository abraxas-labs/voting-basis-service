// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class VoteModelBuilder :
    IEntityTypeConfiguration<Vote>,
    IEntityTypeConfiguration<Ballot>,
    IEntityTypeConfiguration<BallotQuestion>,
    IEntityTypeConfiguration<TieBreakQuestion>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di!.Votes)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.Votes)
            .HasForeignKey(v => v.ContestId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(v => v.Ballots)
            .WithOne(b => b.Vote)
            .HasForeignKey(b => b.VoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(x => x.OfficialDescription)
            .HasJsonConversion();

        builder
            .Property(x => x.ShortDescription)
            .HasJsonConversion();
    }

    public void Configure(EntityTypeBuilder<Ballot> builder)
    {
        builder
            .HasMany(b => b.BallotQuestions)
            .WithOne(bq => bq.Ballot)
            .HasForeignKey(bq => bq.BallotId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(b => b.TieBreakQuestions)
            .WithOne(bq => bq.Ballot)
            .HasForeignKey(bq => bq.BallotId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasIndex(b => new { b.VoteId, b.Position })
            .IsUnique();

        builder
            .Property(x => x.HasTieBreakQuestions)
            .IsRequired();

        builder
            .Property(x => x.Description)
            .HasJsonConversion();
    }

    public void Configure(EntityTypeBuilder<BallotQuestion> builder)
    {
        builder
            .HasIndex(bq => new { bq.Number, bq.BallotId })
            .IsUnique();

        builder
            .Property(x => x.Number)
            .IsRequired();

        builder
            .Property(x => x.Question)
            .HasJsonConversion()
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<TieBreakQuestion> builder)
    {
        builder
            .HasIndex(tbq => new { tbq.BallotId, QuestionNumber1 = tbq.Question1Number, QuestionNumber2 = tbq.Question2Number })
            .IsUnique();

        builder
            .Property(x => x.Question)
            .HasJsonConversion();

        builder
            .Property(tbq => tbq.Question1Number)
            .IsRequired();

        builder
            .Property(tbq => tbq.Question2Number)
            .IsRequired();
    }
}
