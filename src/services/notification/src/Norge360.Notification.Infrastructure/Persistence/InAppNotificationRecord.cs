// <copyright file="InAppNotificationRecord.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Notification.Contracts.Notifications.Enums;

namespace Norge360.Notification.Infrastructure.Persistence;

public sealed class InAppNotificationRecord
{
    private InAppNotificationRecord()
    {
        Subject = string.Empty;
        Body = string.Empty;
        Type = string.Empty;
        Category = NotificationCategory.System;
        MetadataJson = "{}";
    }

    public InAppNotificationRecord(
        Guid id,
        Guid userId,
        NotificationCategory category,
        string type,
        string subject,
        string body,
        string? url,
        Guid? actorUserId,
        string? actorUsername,
        string? actorDisplayName,
        string? actorAvatarUrl,
        string? entityType,
        string? entityId,
        string metadataJson,
        string? correlationId,
        DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Category = category;
        Type = type;
        Subject = subject;
        Body = body;
        Url = url;
        ActorUserId = actorUserId;
        ActorUsername = actorUsername;
        ActorDisplayName = actorDisplayName;
        ActorAvatarUrl = actorAvatarUrl;
        EntityType = entityType;
        EntityId = entityId;
        MetadataJson = metadataJson;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public string Type { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public string? Url { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorUsername { get; private set; }
    public string? ActorDisplayName { get; private set; }
    public string? ActorAvatarUrl { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string MetadataJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public void MarkRead(DateTime readAtUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = readAtUtc;
    }
}
