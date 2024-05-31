// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class PoliticalAssemblyModelBuilder :
    IEntityTypeConfiguration<PoliticalAssembly>
{
    public void Configure(EntityTypeBuilder<PoliticalAssembly> builder)
    {
        builder
            .Property(d => d.Date)
            .HasDateType()
            .HasUtcConversion();

        builder
            .HasOne(c => c.DomainOfInfluence)
            .WithMany(doi => doi.PoliticalAssemblies)
            .HasForeignKey(c => c.DomainOfInfluenceId)
            .IsRequired();

        builder
            .Property(x => x.Description)
            .HasJsonConversion();
    }
}
