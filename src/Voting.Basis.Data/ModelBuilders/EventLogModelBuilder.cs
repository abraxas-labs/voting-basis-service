// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.ModelBuilders;

public class EventLogModelBuilder :
    IEntityTypeConfiguration<EventLog>,
    IEntityTypeConfiguration<EventLogUser>,
    IEntityTypeConfiguration<EventLogTenant>
{
    public void Configure(EntityTypeBuilder<EventLog> builder)
    {
        builder
            .HasOne(x => x.EventUser)
            .WithMany(x => x!.EventLogs)
            .HasForeignKey(x => x.EventUserId)
            .IsRequired();

        builder
            .HasOne(x => x.EventTenant)
            .WithMany(x => x!.EventLogs)
            .HasForeignKey(x => x.EventTenantId)
            .IsRequired();

        builder
            .Property(x => x.Timestamp)
            .HasUtcConversion();
    }

    public void Configure(EntityTypeBuilder<EventLogUser> builder)
    {
        builder
            .HasIndex(x => new { x.UserId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<EventLogTenant> builder)
    {
        builder
            .HasIndex(x => new { x.TenantId })
            .IsUnique();
    }
}
