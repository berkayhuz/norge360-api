// <copyright file="DistributedActiveConversationRegistry.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Caching.Distributed;
using Norge360.MessagingService.Application.Abstractions;

namespace Norge360.MessagingService.Infrastructure.Services;

internal sealed class DistributedActiveConversationRegistry(IDistributedCache cache) : IActiveConversationRegistry
{
    public Task MarkActiveAsync(Guid userId, Guid conversationId, TimeSpan ttl, CancellationToken cancellationToken) =>
        cache.SetStringAsync(
            BuildKey(userId, conversationId),
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

    public Task ClearActiveAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        cache.RemoveAsync(BuildKey(userId, conversationId), cancellationToken);

    public async Task<bool> IsActiveAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        await cache.GetStringAsync(BuildKey(userId, conversationId), cancellationToken) is not null;

    private static string BuildKey(Guid userId, Guid conversationId) =>
        $"messaging:active-conversation:{userId:D}:{conversationId:D}";
}
