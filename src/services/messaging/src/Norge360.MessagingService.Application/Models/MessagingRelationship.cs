// <copyright file="MessagingRelationship.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Application.Models;

public sealed record MessagingRelationship(
    bool IsFollowing,
    bool IsFollowedBy,
    bool IsBlockedByRequester,
    bool IsBlockedByTarget)
{
    public bool IsMutual => IsFollowing && IsFollowedBy;
}
