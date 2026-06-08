// <copyright file="MessagingConversationConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingConversationConfiguration : IEntityTypeConfiguration<MessagingConversation>
{
    public void Configure(EntityTypeBuilder<MessagingConversation> builder)
    {
        builder.ToTable("MessagingConversations");
        builder.HasKey(static conversation => conversation.Id);
        builder.Property(static conversation => conversation.Kind).IsRequired();
        builder.Property(static conversation => conversation.Status).IsRequired();
        builder.Property(static conversation => conversation.RequestStatus).IsRequired();
        builder.Property(static conversation => conversation.CreatedByUserId).IsRequired();
        builder.Property(static conversation => conversation.CreatedAtUtc).IsRequired();
        builder.Property(static conversation => conversation.UpdatedAtUtc).IsRequired();

        builder.HasIndex(static conversation => new { conversation.Status, conversation.LastMessageAtUtc });
        builder.HasIndex(static conversation => conversation.CreatedByUserId);

        builder.HasMany(static conversation => conversation.Participants)
            .WithOne(static participant => participant.Conversation)
            .HasForeignKey(static participant => participant.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(static conversation => conversation.Messages)
            .WithOne(static message => message.Conversation)
            .HasForeignKey(static message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
