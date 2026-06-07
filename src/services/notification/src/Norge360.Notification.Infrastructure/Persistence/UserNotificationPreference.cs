// <copyright file="UserNotificationPreference.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Notification.Infrastructure.Persistence;

public sealed class UserNotificationPreference
{
    private UserNotificationPreference()
    {
        Type = string.Empty;
    }

    public UserNotificationPreference(
        Guid id,
        Guid userId,
        string type,
        bool inAppEnabled,
        bool emailEnabled,
        bool pushEnabled,
        DateTime updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Type = type;
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(bool? inAppEnabled, bool? emailEnabled, bool? pushEnabled, DateTime updatedAtUtc)
    {
        if (inAppEnabled.HasValue)
        {
            InAppEnabled = inAppEnabled.Value;
        }

        if (emailEnabled.HasValue)
        {
            EmailEnabled = emailEnabled.Value;
        }

        if (pushEnabled.HasValue)
        {
            PushEnabled = pushEnabled.Value;
        }

        UpdatedAtUtc = updatedAtUtc;
    }
}
