// <copyright file="IMessagingRealtimePublisher.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Contracts.Responses;

namespace Norge360.MessagingService.Application.Abstractions;

public interface IMessagingRealtimePublisher
{
    Task MessageCreatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken);
    Task MessageUpdatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken);
    Task ConversationUpdatedAsync(Guid conversationId, ConversationSummaryResponse conversation, CancellationToken cancellationToken);
    Task ReadReceiptUpdatedAsync(Guid conversationId, Guid userId, Guid? messageId, DateTimeOffset readAtUtc, CancellationToken cancellationToken);
    Task TypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken);
    Task PresenceUpdatedAsync(Guid conversationId, Guid userId, bool isOnline, CancellationToken cancellationToken);
}
