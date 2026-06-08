// <copyright file="IMessagingDbContext.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Application.Abstractions;

public interface IMessagingDbContext
{
    DbSet<MessagingConversation> Conversations { get; }
    DbSet<MessagingConversationParticipant> ConversationParticipants { get; }
    DbSet<MessagingMessage> Messages { get; }
    DbSet<MessagingMessageAttachment> MessageAttachments { get; }
    DbSet<MessagingMessageReaction> MessageReactions { get; }
    DbSet<MessagingMessageReceipt> MessageReceipts { get; }
    DbSet<MessagingUserSettings> UserSettings { get; }
    DbSet<MessagingUserDevice> UserDevices { get; }
    DbSet<MessagingConversationReport> Reports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
