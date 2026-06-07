// <copyright file="EfUserNotificationPreferenceService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Notification.Application.Abstractions;
using Norge360.Notification.Contracts.Notifications.Enums;
using Norge360.Notification.Contracts.Notifications.Requests;
using Norge360.Notification.Contracts.Notifications.Responses;
using Norge360.Notification.Infrastructure.Persistence;

namespace Norge360.Notification.Infrastructure.Services;

public sealed class EfUserNotificationPreferenceService(NotificationDbContext dbContext) :
    IUserNotificationPreferenceService,
    IUserNotificationPreferenceReader
{
    public async Task<bool> IsChannelEnabledAsync(
        Guid userId,
        NotificationCategory category,
        string? notificationType,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        if (category == NotificationCategory.Security &&
            !string.Equals(notificationType, "security.suspicious_login", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var definition = NotificationPreferenceCatalog.Resolve(category, notificationType);
        var preference = await dbContext.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.Type == definition.Type,
                cancellationToken);

        return channel switch
        {
            NotificationChannel.InApp => preference?.InAppEnabled ?? definition.InAppEnabled,
            NotificationChannel.Email => preference?.EmailEnabled ?? definition.EmailEnabled,
            NotificationChannel.Push => preference?.PushEnabled ?? definition.PushEnabled,
            NotificationChannel.Sms => false,
            _ => false
        };
    }

    public async Task<NotificationPreferencesResponse> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserNotificationPreferences
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .ToDictionaryAsync(item => item.Type, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var items = NotificationPreferenceCatalog.Definitions
            .Select(definition => Map(definition, existing.TryGetValue(definition.Type, out var preference) ? preference : null))
            .ToArray();

        return new NotificationPreferencesResponse(items);
    }

    public async Task<NotificationPreferencesResponse> UpdateAsync(
        Guid userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedTypes = request.Items
            .Select(static item => item.Type.Trim())
            .Where(static type => type.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedTypes.Length == 0)
        {
            return await GetAsync(userId, cancellationToken);
        }

        var existing = await dbContext.UserNotificationPreferences
            .Where(item => item.UserId == userId && requestedTypes.Contains(item.Type))
            .ToDictionaryAsync(item => item.Type, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in request.Items)
        {
            var type = item.Type.Trim();
            if (!NotificationPreferenceCatalog.ByType.TryGetValue(type, out var definition))
            {
                continue;
            }

            if (!existing.TryGetValue(definition.Type, out var preference))
            {
                preference = new UserNotificationPreference(
                    Guid.NewGuid(),
                    userId,
                    definition.Type,
                    definition.InAppEnabled,
                    definition.EmailEnabled,
                    definition.PushEnabled,
                    now);
                dbContext.UserNotificationPreferences.Add(preference);
            }

            preference.Update(item.InAppEnabled, item.EmailEnabled, item.PushEnabled, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    private static NotificationPreferenceResponse Map(
        NotificationPreferenceDefinition definition,
        UserNotificationPreference? preference) =>
        new(
            definition.Type,
            definition.Category.ToString(),
            preference?.InAppEnabled ?? definition.InAppEnabled,
            preference?.EmailEnabled ?? definition.EmailEnabled,
            preference?.PushEnabled ?? definition.PushEnabled);
}
