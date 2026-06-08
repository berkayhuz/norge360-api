// <copyright file="MessagingMessageAttachmentConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingMessageAttachmentConfiguration : IEntityTypeConfiguration<MessagingMessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessagingMessageAttachment> builder)
    {
        builder.ToTable("MessagingMessageAttachments");
        builder.HasKey(static attachment => attachment.Id);
        builder.Property(static attachment => attachment.Kind).IsRequired();
        builder.Property(static attachment => attachment.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(static attachment => attachment.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(static attachment => attachment.EncryptedFileKey).HasColumnType("bytea").IsRequired();
        builder.Property(static attachment => attachment.KeyNonce).HasColumnType("bytea").IsRequired();
        builder.Property(static attachment => attachment.KeyId).HasMaxLength(128).IsRequired();
        builder.Property(static attachment => attachment.CreatedAtUtc).IsRequired();

        builder.HasIndex(static attachment => new { attachment.MessageId, attachment.Kind });
        builder.HasIndex(static attachment => new { attachment.Kind, attachment.CreatedAtUtc });

        builder.HasOne(static attachment => attachment.Message)
            .WithMany(static message => message.Attachments)
            .HasForeignKey(static attachment => attachment.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
