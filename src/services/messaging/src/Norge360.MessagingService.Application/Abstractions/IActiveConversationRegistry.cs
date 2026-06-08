// <copyright file="IActiveConversationRegistry.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Application.Abstractions;

public interface IActiveConversationRegistry
{
    Task MarkActiveAsync(Guid userId, Guid conversationId, TimeSpan ttl, CancellationToken cancellationToken);
    Task ClearActiveAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
}
