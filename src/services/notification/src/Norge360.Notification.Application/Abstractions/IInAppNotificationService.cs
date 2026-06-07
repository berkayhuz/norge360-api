// <copyright file="IInAppNotificationService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Notification.Contracts.Notifications.Responses;

namespace Norge360.Notification.Application.Abstractions;

public interface IInAppNotificationService
{
    Task<InAppNotificationsPageResponse> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        bool markAsSeen,
        CancellationToken cancellationToken);

    Task<NotificationSummaryResponse> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task MarkAllAsSeenAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
