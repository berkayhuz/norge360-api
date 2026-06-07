// <copyright file="EfInAppNotificationService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Norge360.Notification.Application.Abstractions;
using Norge360.Notification.Contracts.Notifications.Responses;
using Norge360.Notification.Infrastructure.Persistence;

namespace Norge360.Notification.Infrastructure.Services;

public sealed class EfInAppNotificationService(NotificationDbContext dbContext) : IInAppNotificationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<InAppNotificationsPageResponse> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        bool markAsSeen,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 50);
        var total = await dbContext.InAppNotifications
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId, cancellationToken);
        var rows = await dbContext.InAppNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToArrayAsync(cancellationToken);

        if (markAsSeen)
        {
            await MarkAllAsSeenAsync(userId, cancellationToken);
        }

        return new InAppNotificationsPageResponse(
            rows.Select(Map).ToArray(),
            safePage,
            safePageSize,
            total,
            safePage * safePageSize < total);
    }

    public async Task<NotificationSummaryResponse> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var unseenCount = await dbContext.InAppNotifications
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);
        var lastSeenAtUtc = await dbContext.InAppNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.ReadAtUtc != null)
            .MaxAsync(item => item.ReadAtUtc, cancellationToken);

        return new NotificationSummaryResponse(unseenCount, lastSeenAtUtc);
    }

    public Task MarkAllAsSeenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return dbContext.InAppNotifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.IsRead, true)
                    .SetProperty(item => item.ReadAtUtc, now),
                cancellationToken);
    }

    private static InAppNotificationResponse Map(InAppNotificationRecord row) =>
        new(
            row.Id,
            row.Type,
            row.Category.ToString(),
            row.Subject,
            row.Body,
            row.Url,
            row.ActorUserId,
            row.ActorUsername,
            row.ActorDisplayName,
            row.ActorAvatarUrl,
            row.EntityType,
            row.EntityId,
            DeserializeMetadata(row.MetadataJson),
            row.CreatedAtUtc);

    private static IReadOnlyDictionary<string, string> DeserializeMetadata(string metadataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(metadataJson, SerializerOptions)
                ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
