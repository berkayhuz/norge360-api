// <copyright file="NotificationPreferenceCatalog.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Notification.Contracts.Notifications;
using Norge360.Notification.Contracts.Notifications.Enums;

namespace Norge360.Notification.Infrastructure.Services;

internal static class NotificationPreferenceCatalog
{
    public static readonly IReadOnlyList<NotificationPreferenceDefinition> Definitions =
    [
        new(NotificationTypes.NewFollower, NotificationCategory.Social, true, false, false),
        new(NotificationTypes.FollowRequest, NotificationCategory.Social, true, false, false),
        new(NotificationTypes.FollowRequestAccepted, NotificationCategory.Social, true, false, false),
        new(NotificationTypes.ProfilePost, NotificationCategory.Community, true, false, false),
        new(NotificationTypes.CityPost, NotificationCategory.Community, true, false, false),
        new(NotificationTypes.FollowedFirstPost, NotificationCategory.Community, true, false, false),
        new(NotificationTypes.PostLike, NotificationCategory.Community, true, false, false),
        new(NotificationTypes.CommentLike, NotificationCategory.Community, true, false, false),
        new(NotificationTypes.PostComment, NotificationCategory.Community, true, false, false),
        new(NotificationTypes.CommentReply, NotificationCategory.Community, true, false, false),
        new("security.suspicious_login", NotificationCategory.Security, true, false, false)
    ];

    public static IReadOnlyDictionary<string, NotificationPreferenceDefinition> ByType { get; } =
        Definitions.ToDictionary(static item => item.Type, StringComparer.OrdinalIgnoreCase);

    public static NotificationPreferenceDefinition Resolve(
        NotificationCategory category,
        string? type)
    {
        if (!string.IsNullOrWhiteSpace(type) && ByType.TryGetValue(type, out var definition))
        {
            return definition;
        }

        return new NotificationPreferenceDefinition(
            type ?? category.ToString(),
            category,
            InAppEnabled: true,
            EmailEnabled: false,
            PushEnabled: false);
    }
}

internal sealed record NotificationPreferenceDefinition(
    string Type,
    NotificationCategory Category,
    bool InAppEnabled,
    bool EmailEnabled,
    bool PushEnabled);
