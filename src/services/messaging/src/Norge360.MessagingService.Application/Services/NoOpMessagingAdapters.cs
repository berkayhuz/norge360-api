// <copyright file="NoOpMessagingAdapters.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Application.Models;
using Norge360.MessagingService.Contracts.Responses;

namespace Norge360.MessagingService.Application.Services;

internal sealed class NoOpMessagingRealtimePublisher : IMessagingRealtimePublisher
{
    public Task MessageCreatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task MessageUpdatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ConversationUpdatedAsync(Guid conversationId, ConversationSummaryResponse conversation, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReadReceiptUpdatedAsync(Guid conversationId, Guid userId, Guid? messageId, DateTimeOffset readAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task TypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PresenceUpdatedAsync(Guid conversationId, Guid userId, bool isOnline, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class NoOpActiveConversationRegistry : IActiveConversationRegistry
{
    public Task MarkActiveAsync(Guid userId, Guid conversationId, TimeSpan ttl, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ClearActiveAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<bool> IsActiveAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) => Task.FromResult(false);
}

internal sealed class AllowAllRelationshipReader : IUserRelationshipReader
{
    public Task<MessagingRelationship> GetAsync(Guid requesterUserId, Guid targetUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MessagingRelationship(IsFollowing: true, IsFollowedBy: true, IsBlockedByRequester: false, IsBlockedByTarget: false));
}
