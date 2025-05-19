// (c) Copyright by Abraxas Informatik AG
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
    }
}
