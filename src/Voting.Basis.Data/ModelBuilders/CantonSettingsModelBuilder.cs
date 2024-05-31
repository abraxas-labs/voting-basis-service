// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class CantonSettingsModelBuilder :
    IEntityTypeConfiguration<CantonSettings>,
    IEntityTypeConfiguration<CantonSettingsVotingCardChannel>,
    IEntityTypeConfiguration<CountingCircleResultStateDescription>
{
    public void Configure(EntityTypeBuilder<CantonSettings> builder)
    {
        builder.HasIndex(x => x.Canton)
            .IsUnique();

        builder.Property(x => x.ProportionalElectionMandateAlgorithms);

        builder.Property(x => x.SwissAbroadVotingRightDomainOfInfluenceTypes);

        builder.Property(x => x.EnabledPoliticalBusinessUnionTypes);

        builder
            .HasMany(x => x.EnabledVotingCardChannels)
            .WithOne(x => x.CantonSettings)
            .HasForeignKey(x => x.CantonSettingsId);

        builder
            .HasMany(x => x.CountingCircleResultStateDescriptions)
            .WithOne(x => x.CantonSettings)
            .HasForeignKey(x => x.CantonSettingsId);
    }

    public void Configure(EntityTypeBuilder<CantonSettingsVotingCardChannel> builder)
    {
        builder
            .HasIndex(x => new { x.CantonSettingsId, x.Valid, Channel = x.VotingChannel })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<CountingCircleResultStateDescription> builder)
    {
        builder.HasIndex(x => new { x.CantonSettingsId, x.State }).IsUnique();
    }
}
