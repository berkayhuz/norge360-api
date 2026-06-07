// <copyright file="UserProfileNotificationSubscriptionConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class UserProfileNotificationSubscriptionConfiguration : IEntityTypeConfiguration<UserProfileNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<UserProfileNotificationSubscription> builder)
    {
        builder.ToTable(
            "UserProfileNotificationSubscriptions",
            table => table.HasCheckConstraint(
                "CK_UserProfileNotificationSubscriptions_Subscriber_NotEqual_Target",
                "\"SubscriberProfileId\" <> \"TargetProfileId\""));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.SubscriberProfileId, x.TargetProfileId }).IsUnique();
        builder.HasIndex(x => x.TargetProfileId);
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.SubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.TargetProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
