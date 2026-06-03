// <copyright file="DiscoverySubjectSnapshotConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Discovery.Domain.Entities;

namespace Norge360.Discovery.Infrastructure.Persistence.Configurations;

public sealed class DiscoverySubjectSnapshotConfiguration : IEntityTypeConfiguration<DiscoverySubjectSnapshot>
{
    public void Configure(EntityTypeBuilder<DiscoverySubjectSnapshot> builder)
    {
        builder.ToTable("DiscoverySubjectSnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SubjectType).HasConversion<short>();
        builder.Property(x => x.Username).HasMaxLength(64);
        builder.Property(x => x.DisplayName).HasMaxLength(160);
        builder.Property(x => x.AvatarUrl).HasMaxLength(2048);
        builder.Property(x => x.Bio).HasMaxLength(500);
        builder.Property(x => x.FollowersCount).HasDefaultValue(0);
        builder.Property(x => x.PostsCount).HasDefaultValue(0);
        builder.Property(x => x.Visibility).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.SubjectType, x.SubjectId }).IsUnique();
        builder.HasIndex(x => new { x.SubjectType, x.IsActive, x.IsDeleted, x.Visibility });
        builder.HasIndex(x => new { x.SubjectType, x.IsActive, x.IsDeleted, x.Visibility, x.FollowersCount, x.PostsCount });
    }
}
