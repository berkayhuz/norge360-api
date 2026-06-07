// <copyright file="InAppNotificationChannelSender.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Norge360.Notification.Application.Abstractions;
using Norge360.Notification.Contracts.Notifications.Enums;
using Norge360.Notification.Contracts.Notifications.Requests;
using Norge360.Notification.Infrastructure.Persistence;

namespace Norge360.Notification.Infrastructure.Channels;

public sealed class InAppNotificationChannelSender(NotificationDbContext dbContext) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.InApp;
    public string ProviderName => "notification-db";

    public async Task<NotificationChannelSendResult> SendAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Recipient.UserId is null)
        {
            return new NotificationChannelSendResult(
                Succeeded: false,
                ExternalMessageId: null,
                ErrorCode: "in_app_recipient_missing",
                ErrorMessage: "In-App channel requires recipient user id.");
        }

        var record = new InAppNotificationRecord(
            Guid.NewGuid(),
            request.Recipient.UserId.Value,
            request.Category,
            ResolveNotificationType(request),
            request.Subject,
            request.TextBody,
            Normalize(GetMetadata(request, "url"), 1024),
            TryGetGuid(GetMetadata(request, "actorUserId")),
            Normalize(GetMetadata(request, "actorUsername"), 128),
            Normalize(GetMetadata(request, "actorDisplayName"), 256),
            Normalize(GetMetadata(request, "actorAvatarUrl"), 1024),
            Normalize(GetMetadata(request, "entityType"), 128),
            Normalize(GetMetadata(request, "entityId"), 128),
            JsonSerializer.Serialize(request.Metadata),
            request.CorrelationId,
            DateTime.UtcNow);

        await dbContext.InAppNotifications.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new NotificationChannelSendResult(true, record.Id.ToString("D"), null, null);
    }

    private static string? GetMetadata(SendNotificationRequest request, string key) =>
        request.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static string ResolveNotificationType(SendNotificationRequest request) =>
        Normalize(GetMetadata(request, "notificationType") ?? request.TemplateKey ?? request.Category.ToString(), 128)
            ?? request.Category.ToString();

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static Guid? TryGetGuid(string? value) =>
        Guid.TryParse(value, out var result) ? result : null;
}
