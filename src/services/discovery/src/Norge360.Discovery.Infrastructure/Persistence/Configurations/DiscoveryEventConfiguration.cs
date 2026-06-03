// <copyright file="DiscoveryEventConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Discovery.Domain.Entities;

namespace Norge360.Discovery.Infrastructure.Persistence.Configurations;

public sealed class DiscoveryEventConfiguration : IEntityTypeConfiguration<DiscoveryEvent>
{
    public void Configure(EntityTypeBuilder<DiscoveryEvent> builder)
    {
        builder.ToTable("DiscoveryEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasConversion<short>();
        builder.Property(x => x.SourceService).HasMaxLength(80).IsRequired();
        builder.Property(x => x.SourceEntityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.SourceEntityId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TargetEntityType).HasMaxLength(80);
        builder.Property(x => x.TargetEntityId).HasMaxLength(128);
        builder.Property(x => x.DeduplicationKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.InvalidReason).HasMaxLength(128);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.DeduplicationKey).IsUnique();
        builder.HasIndex(x => new { x.TargetProfileId, x.OccurredAt });
        builder.HasIndex(x => new { x.EventType, x.OccurredAt });
        builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId, x.ActorUserId });
    }
}
