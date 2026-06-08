// <copyright file="MessagingUserDeviceConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingUserDeviceConfiguration : IEntityTypeConfiguration<MessagingUserDevice>
{
    public void Configure(EntityTypeBuilder<MessagingUserDevice> builder)
    {
        builder.ToTable("MessagingUserDevices");
        builder.HasKey(static device => device.Id);
        builder.Property(static device => device.DeviceId).HasMaxLength(128).IsRequired();
        builder.Property(static device => device.PublicIdentityKey).HasMaxLength(512).IsRequired();
        builder.Property(static device => device.SignedPreKey).HasMaxLength(512).IsRequired();
        builder.Property(static device => device.SignedPreKeySignature).HasMaxLength(512).IsRequired();
        builder.Property(static device => device.SupportedAlgorithms).HasMaxLength(256).IsRequired();
        builder.Property(static device => device.CreatedAtUtc).IsRequired();
        builder.HasIndex(static device => new { device.UserId, device.DeviceId }).IsUnique();
        builder.HasIndex(static device => new { device.UserId, device.RevokedAtUtc });
    }
}
