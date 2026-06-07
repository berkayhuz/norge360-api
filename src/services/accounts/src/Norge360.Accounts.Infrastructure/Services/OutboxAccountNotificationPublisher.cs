// <copyright file="OutboxAccountNotificationPublisher.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Notification.Contracts.IntegrationEvents.V1;
using Norge360.Notification.Contracts.Notifications;
using Norge360.Notification.Contracts.Notifications.Enums;
using Norge360.Notification.Contracts.Notifications.Models;

namespace Norge360.Accounts.Infrastructure.Services;

public sealed class OutboxAccountNotificationPublisher(IIntegrationEventOutbox outbox) : IAccountNotificationPublisher
{
    public Task PublishFollowedAsync(
        UserProfile followerProfile,
        UserProfile followeeProfile,
        Guid followId,
        CancellationToken cancellationToken) =>
        EnqueueAsync(
            followeeProfile.AuthUserId,
            followerProfile,
            NotificationTypes.NewFollower,
            "Yeni takipçin var",
            $"{DisplayName(followerProfile)} seni takip etmeye başladı.",
            $"/{followerProfile.Username}",
            "UserFollow",
            followId.ToString("D"),
            $"accounts:followed:{followId:D}",
            cancellationToken);

    public Task PublishFollowRequestAsync(
        UserProfile followerProfile,
        UserProfile followeeProfile,
        Guid followId,
        CancellationToken cancellationToken) =>
        EnqueueAsync(
            followeeProfile.AuthUserId,
            followerProfile,
            NotificationTypes.FollowRequest,
            "Yeni takip isteği",
            $"{DisplayName(followerProfile)} seni takip etmek istiyor.",
            $"/{followerProfile.Username}",
            "UserFollow",
            followId.ToString("D"),
            $"accounts:follow-request:{followId:D}",
            cancellationToken);

    public Task PublishFollowRequestAcceptedAsync(
        UserProfile followerProfile,
        UserProfile followeeProfile,
        Guid followId,
        CancellationToken cancellationToken) =>
        EnqueueAsync(
            followerProfile.AuthUserId,
            followeeProfile,
            NotificationTypes.FollowRequestAccepted,
            "Takip isteğin kabul edildi",
            $"{DisplayName(followeeProfile)} takip isteğini kabul etti.",
            $"/{followeeProfile.Username}",
            "UserFollow",
            followId.ToString("D"),
            $"accounts:follow-accepted:{followId:D}",
            cancellationToken);

    private Task EnqueueAsync(
        Guid recipientUserId,
        UserProfile actorProfile,
        string notificationType,
        string subject,
        string textBody,
        string url,
        string entityType,
        string entityId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var eventId = Guid.NewGuid();
        var metadata = new Dictionary<string, string>
        {
            ["notificationType"] = notificationType,
            ["url"] = url,
            ["actorUserId"] = actorProfile.AuthUserId.ToString("D"),
            ["actorUsername"] = actorProfile.Username,
            ["actorDisplayName"] = DisplayName(actorProfile),
            ["entityType"] = entityType,
            ["entityId"] = entityId
        };

        if (!string.IsNullOrWhiteSpace(actorProfile.AvatarUrl))
        {
            metadata["actorAvatarUrl"] = actorProfile.AvatarUrl;
        }

        var payload = new NotificationRequestedV1(
            eventId,
            recipientUserId,
            "Accounts",
            NotificationCategory.Social,
            NotificationPriority.Normal,
            new NotificationRecipient(recipientUserId, null, null, null, null),
            [NotificationChannel.InApp],
            subject,
            textBody,
            null,
            new NotificationTemplateData(notificationType, metadata),
            metadata,
            null,
            idempotencyKey,
            now);

        return outbox.AddAsync(
            eventId,
            NotificationRequestedV1.EventName,
            NotificationRequestedV1.EventVersion,
            NotificationRequestedV1.RoutingKey,
            "Accounts",
            payload,
            null,
            null,
            now,
            cancellationToken);
    }

    private static string DisplayName(UserProfile profile) =>
        string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Username : profile.DisplayName.Trim();
}
