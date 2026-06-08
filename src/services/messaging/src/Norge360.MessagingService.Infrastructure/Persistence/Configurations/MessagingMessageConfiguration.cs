// <copyright file="MessagingMessageConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingMessageConfiguration : IEntityTypeConfiguration<MessagingMessage>
{
    public void Configure(EntityTypeBuilder<MessagingMessage> builder)
    {
        builder.ToTable("MessagingMessages");
        builder.HasKey(static message => message.Id);
        builder.Property(static message => message.SenderDeviceId).HasMaxLength(128).IsRequired();
        builder.Property(static message => message.ClientMessageId).HasMaxLength(128).IsRequired();
        builder.Property(static message => message.Kind).IsRequired();
        builder.Property(static message => message.State).IsRequired();
        builder.Property(static message => message.CipherText).HasColumnType("bytea").IsRequired();
        builder.Property(static message => message.CipherNonce).HasColumnType("bytea").IsRequired();
        builder.Property(static message => message.CipherKeyId).HasMaxLength(128).IsRequired();
        builder.Property(static message => message.EncryptionAlgorithm).HasMaxLength(128).IsRequired();
        builder.Property(static message => message.ClientSearchTokenHash).HasMaxLength(128);
        builder.Property(static message => message.CreatedAtUtc).IsRequired();
        builder.Property(static message => message.SentAtUtc).IsRequired();
        builder.Property(static message => message.EditUntilUtc).IsRequired();
        builder.Property(static message => message.RecallUntilUtc).IsRequired();

        builder.HasIndex(static message => new { message.ConversationId, message.CreatedAtUtc });
        builder.HasIndex(static message => new { message.ConversationId, message.SenderUserId, message.ClientMessageId }).IsUnique();
        builder.HasIndex(static message => new { message.ConversationId, message.ClientSearchTokenHash });
        builder.HasIndex(static message => message.ExpiresAtUtc);
        builder.HasIndex(static message => message.SharedPostId);
        builder.HasIndex(static message => message.ReplyToMessageId);
    }
}
