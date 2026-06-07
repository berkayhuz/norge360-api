// <copyright file="UserProfileConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthUserId).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NormalizedUsername).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100);
        builder.Property(x => x.Bio).HasMaxLength(500);
        builder.Property(x => x.AvatarUrl).HasMaxLength(512);
        builder.Property(x => x.AvatarStorageKey).HasMaxLength(512);
        builder.Property(x => x.CoverPhotoUrl).HasMaxLength(512);
        builder.Property(x => x.CoverPhotoStorageKey).HasMaxLength(512);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.Occupation).HasMaxLength(100);
        builder.Property(x => x.Company).HasMaxLength(100);
        builder.Property(x => x.Website).HasMaxLength(256);
        builder.Property(x => x.FollowersCount).HasDefaultValue(0);
        builder.Property(x => x.FollowingCount).HasDefaultValue(0);
        builder.Property(x => x.PostsCount).HasDefaultValue(0);
        builder.Property(x => x.IsVerified).HasDefaultValue(false);
        builder.Property(x => x.AccountType).HasConversion<short>();
        builder.Property(x => x.ProfileVisibility).HasConversion<short>();
        builder.Property(x => x.CommentAudience).HasConversion<short>().HasDefaultValue(Domain.Enums.PostCommentAudience.Followers);
        builder.Property(x => x.HideLikeCounts).HasDefaultValue(false);
        builder.HasIndex(x => x.AuthUserId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.NormalizedUsername)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.IsVerified)
            .HasFilter("\"IsVerified\" = true AND \"IsDeleted\" = false");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
