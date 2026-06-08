// <copyright file="MessagingMessageReactionConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingMessageReactionConfiguration : IEntityTypeConfiguration<MessagingMessageReaction>
{
    public void Configure(EntityTypeBuilder<MessagingMessageReaction> builder)
    {
        builder.ToTable("MessagingMessageReactions");
        builder.HasKey(static reaction => reaction.Id);
        builder.Property(static reaction => reaction.Emoji).HasMaxLength(32).IsRequired();
        builder.Property(static reaction => reaction.CreatedAtUtc).IsRequired();
        builder.HasIndex(static reaction => new { reaction.MessageId, reaction.UserId, reaction.Emoji }).IsUnique();

        builder.HasOne(static reaction => reaction.Message)
            .WithMany(static message => message.Reactions)
            .HasForeignKey(static reaction => reaction.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
