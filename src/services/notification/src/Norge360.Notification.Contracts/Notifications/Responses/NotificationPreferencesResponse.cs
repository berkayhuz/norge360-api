// <copyright file="NotificationPreferencesResponse.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Notification.Contracts.Notifications.Responses;

public sealed record NotificationPreferenceResponse(
    string Type,
    string Category,
    bool InAppEnabled,
    bool EmailEnabled,
    bool PushEnabled);

public sealed record NotificationPreferencesResponse(
    IReadOnlyList<NotificationPreferenceResponse> Items);
