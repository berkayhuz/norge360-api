// <copyright file="MessagingConversationParticipantConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingConversationParticipantConfiguration : IEntityTypeConfiguration<MessagingConversationParticipant>
{
    public void Configure(EntityTypeBuilder<MessagingConversationParticipant> builder)
    {
        builder.ToTable("MessagingConversationParticipants");
        builder.HasKey(static participant => participant.Id);
        builder.Property(static participant => participant.ConversationId).IsRequired();
        builder.Property(static participant => participant.UserId).IsRequired();
        builder.Property(static participant => participant.Role).IsRequired();
        builder.Property(static participant => participant.JoinedAtUtc).IsRequired();
        builder.Property(static participant => participant.NotificationSoundEnabled).HasDefaultValue(true).IsRequired();
        builder.Property(static participant => participant.NicknameCipherText).HasColumnType("bytea");
        builder.Property(static participant => participant.NicknameNonce).HasColumnType("bytea");
        builder.Property(static participant => participant.NicknameKeyId).HasMaxLength(128);
        builder.Property(static participant => participant.ThemeKey).HasMaxLength(64);
        builder.Property(static participant => participant.BackgroundKey).HasMaxLength(128);
        builder.Ignore(static participant => participant.IsActive);

        builder.HasIndex(static participant => new { participant.ConversationId, participant.UserId }).IsUnique();
        builder.HasIndex(static participant => new { participant.UserId, participant.PinnedAtUtc, participant.ArchivedAtUtc });
        builder.HasIndex(static participant => new { participant.UserId, participant.DeletedAtUtc, participant.LeftAtUtc });
    }
}
