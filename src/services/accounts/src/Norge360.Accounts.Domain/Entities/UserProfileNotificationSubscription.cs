// <copyright file="UserProfileNotificationSubscription.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Domain.Entities;

public sealed class UserProfileNotificationSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriberProfileId { get; set; }
    public Guid TargetProfileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
