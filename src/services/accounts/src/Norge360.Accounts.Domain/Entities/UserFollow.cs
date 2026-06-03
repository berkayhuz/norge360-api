// <copyright file="UserFollow.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Enums;

namespace Norge360.Accounts.Domain.Entities;

public sealed class UserFollow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FollowerId { get; set; }
    public Guid FolloweeId { get; set; }
    public FollowStatus Status { get; set; } = FollowStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
}
