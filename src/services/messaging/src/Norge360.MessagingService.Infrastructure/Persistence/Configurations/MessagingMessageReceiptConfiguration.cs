// <copyright file="MessagingMessageReceiptConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingMessageReceiptConfiguration : IEntityTypeConfiguration<MessagingMessageReceipt>
{
    public void Configure(EntityTypeBuilder<MessagingMessageReceipt> builder)
    {
        builder.ToTable("MessagingMessageReceipts");
        builder.HasKey(static receipt => receipt.Id);
        builder.HasIndex(static receipt => new { receipt.MessageId, receipt.UserId }).IsUnique();
        builder.HasIndex(static receipt => new { receipt.UserId, receipt.ReadAtUtc });

        builder.HasOne(static receipt => receipt.Message)
            .WithMany(static message => message.Receipts)
            .HasForeignKey(static receipt => receipt.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
