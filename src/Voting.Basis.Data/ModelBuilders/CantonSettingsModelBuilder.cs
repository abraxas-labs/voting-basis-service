// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class CantonSettingsModelBuilder :
    IEntityTypeConfiguration<CantonSettings>,
    IEntityTypeConfiguration<CantonSettingsVotingCardChannel>
{
    public void Configure(EntityTypeBuilder<CantonSettings> builder)
    {
        builder.HasIndex(x => x.Canton)
            .IsUnique();

        builder.Property(x => x.ProportionalElectionMandateAlgorithms)
            .HasPostgresEnumListToIntListConversion();

        builder.Property(x => x.SwissAbroadVotingRightDomainOfInfluenceTypes)
            .HasPostgresEnumListToIntListConversion();

        builder.Property(x => x.EnabledPoliticalBusinessUnionTypes)
            .HasPostgresEnumListToIntListConversion();

        builder
            .HasMany(x => x.EnabledVotingCardChannels)
            .WithOne(x => x.CantonSettings)
            .HasForeignKey(x => x.CantonSettingsId);
    }

    public void Configure(EntityTypeBuilder<CantonSettingsVotingCardChannel> builder)
    {
        builder
            .HasIndex(x => new { x.CantonSettingsId, x.Valid, Channel = x.VotingChannel })
            .IsUnique();
    }
}
