// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class SimplePoliticalBusinessModelBuilder :
    IEntityTypeConfiguration<SimplePoliticalBusiness>
{
    public void Configure(EntityTypeBuilder<SimplePoliticalBusiness> builder)
    {
        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di.SimplePoliticalBusinesses)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.SimplePoliticalBusinesses)
            .HasForeignKey(v => v.ContestId)
            .IsRequired();

        builder
            .Property(x => x.ShortDescription)
            .HasJsonConversion();

        builder
            .Property(x => x.OfficialDescription)
            .HasJsonConversion();
    }
}
