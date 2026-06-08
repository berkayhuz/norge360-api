// <copyright file="MessagingUserSettingsConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingUserSettingsConfiguration : IEntityTypeConfiguration<MessagingUserSettings>
{
    public void Configure(EntityTypeBuilder<MessagingUserSettings> builder)
    {
        builder.ToTable("MessagingUserSettings");
        builder.HasKey(static settings => settings.Id);
        builder.Property(static settings => settings.UserId).IsRequired();
        builder.Property(static settings => settings.MessagePermission).IsRequired();
        builder.Property(static settings => settings.GroupInvitePermission).IsRequired();
        builder.Property(static settings => settings.OnlineVisibility).IsRequired();
        builder.Property(static settings => settings.UpdatedAtUtc).IsRequired();
        builder.HasIndex(static settings => settings.UserId).IsUnique();
    }
}
