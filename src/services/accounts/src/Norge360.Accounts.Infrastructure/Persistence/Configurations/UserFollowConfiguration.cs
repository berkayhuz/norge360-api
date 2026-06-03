// <copyright file="UserFollowConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable(
            "UserFollows",
            table => table.HasCheckConstraint(
                "CK_UserFollows_FollowerId_NotEqual_FolloweeId",
                "\"FollowerId\" <> \"FolloweeId\""));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FollowerId).IsRequired();
        builder.Property(x => x.FolloweeId).IsRequired();
        builder.Property(x => x.Status).HasConversion<short>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.FollowerId, x.FolloweeId }).IsUnique();
        builder.HasIndex(x => x.FollowerId);
        builder.HasIndex(x => x.FolloweeId);
        builder.HasIndex(x => x.Status).HasFilter("\"Status\" = 1");
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
