// <copyright file="SignalRMessagingRealtimePublisher.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.SignalR;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Contracts.Responses;

namespace Norge360.MessagingService.API.Hubs;

public sealed class SignalRMessagingRealtimePublisher(IHubContext<MessagingHub> hubContext) : IMessagingRealtimePublisher
{
    public Task MessageCreatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(MessagingHub.ConversationGroup(conversationId))
            .SendAsync(MessagingRealtimeEventNames.MessageCreated, message, cancellationToken);

    public Task MessageUpdatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(MessagingHub.ConversationGroup(conversationId))
            .SendAsync(MessagingRealtimeEventNames.MessageUpdated, message, cancellationToken);

    public Task ConversationUpdatedAsync(Guid conversationId, ConversationSummaryResponse conversation, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(MessagingHub.ConversationGroup(conversationId))
            .SendAsync(MessagingRealtimeEventNames.ConversationUpdated, conversation, cancellationToken);

    public Task ReadReceiptUpdatedAsync(Guid conversationId, Guid userId, Guid? messageId, DateTimeOffset readAtUtc, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(MessagingHub.ConversationGroup(conversationId))
            .SendAsync(MessagingRealtimeEventNames.ReadReceiptUpdated, new { conversationId, userId, messageId, readAtUtc }, cancellationToken);

    public Task TypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(MessagingHub.ConversationGroup(conversationId))
            .SendAsync(MessagingRealtimeEventNames.Typing, new { conversationId, userId, isTyping }, cancellationToken);

    public Task PresenceUpdatedAsync(Guid conversationId, Guid userId, bool isOnline, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(MessagingHub.ConversationGroup(conversationId))
            .SendAsync(MessagingRealtimeEventNames.Presence, new { conversationId, userId, isOnline }, cancellationToken);
}
