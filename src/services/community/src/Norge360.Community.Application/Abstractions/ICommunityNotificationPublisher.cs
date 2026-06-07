// <copyright file="ICommunityNotificationPublisher.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Application.Models;

namespace Norge360.Community.Application.Abstractions;

public interface ICommunityNotificationPublisher
{
    Task PublishPostCreatedAsync(
        CommunityPostPublishedNotification notification,
        CancellationToken cancellationToken = default);

    Task PublishPostLikedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default);

    Task PublishCommentLikedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default);

    Task PublishPostCommentedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default);

    Task PublishCommentRepliedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default);
}
