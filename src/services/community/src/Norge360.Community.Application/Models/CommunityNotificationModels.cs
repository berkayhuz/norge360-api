// <copyright file="CommunityNotificationModels.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Community.Application.Models;

public sealed record CommunityNotificationActor(
    Guid UserId,
    string? Username,
    string? DisplayName,
    string? AvatarUrl);

public sealed record CommunityPostPublishedNotification(
    Guid PostId,
    string PostSlug,
    string? Caption,
    string? City,
    DateTime CreatedAtUtc,
    bool IsFirstPost,
    CommunityNotificationActor Author);

public sealed record CommunityInteractionNotification(
    Guid EntityId,
    string EntityType,
    Guid RecipientUserId,
    Guid PostId,
    string PostSlug,
    string? Text,
    DateTime OccurredAtUtc,
    CommunityNotificationActor Actor,
    CommunityNotificationActor PostAuthor);

public sealed record CommunityNotificationTargets(
    IReadOnlyCollection<Guid> Followers,
    IReadOnlyCollection<Guid> ProfileSubscribers,
    IReadOnlyCollection<Guid> CityResidents);
