// <copyright file="UpdateNotificationPreferencesRequest.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Notification.Contracts.Notifications.Requests;

public sealed record UpdateNotificationPreferencesRequest(
    IReadOnlyList<UpdateNotificationPreferenceItemRequest> Items);

public sealed record UpdateNotificationPreferenceItemRequest(
    string Type,
    bool? InAppEnabled,
    bool? EmailEnabled,
    bool? PushEnabled);
