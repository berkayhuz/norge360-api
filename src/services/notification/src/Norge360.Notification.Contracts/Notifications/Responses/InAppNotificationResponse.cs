// <copyright file="InAppNotificationResponse.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Notification.Contracts.Notifications.Responses;

public sealed record InAppNotificationResponse(
    Guid Id,
    string Type,
    string Category,
    string Subject,
    string Body,
    string? Url,
    Guid? ActorUserId,
    string? ActorUsername,
    string? ActorDisplayName,
    string? ActorAvatarUrl,
    string? EntityType,
    string? EntityId,
    IReadOnlyDictionary<string, string> Metadata,
    DateTime CreatedAtUtc);

public sealed record InAppNotificationsPageResponse(
    IReadOnlyList<InAppNotificationResponse> Items,
    int Page,
    int PageSize,
    int Total,
    bool HasMore);

public sealed record NotificationSummaryResponse(
    int UnseenCount,
    DateTime? LastSeenAtUtc);
