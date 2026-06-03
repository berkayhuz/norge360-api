// <copyright file="OutboxMessageConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RoutingKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
    }
}
