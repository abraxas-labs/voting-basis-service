// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class ExportConfigurationModelBuilder : IEntityTypeConfiguration<ExportConfiguration>
{
    public void Configure(EntityTypeBuilder<ExportConfiguration> builder)
    {
        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithMany(x => x.ExportConfigurations)
            .HasForeignKey(x => x.DomainOfInfluenceId)
            .IsRequired();
    }
}
