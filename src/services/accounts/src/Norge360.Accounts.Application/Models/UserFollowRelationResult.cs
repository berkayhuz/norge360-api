// <copyright file="UserFollowRelationResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record UserFollowRelationResult(
    UserFollowListStatus Status,
    bool IsFollowing,
    bool IsFollowedBy,
    bool IsFollowRequestPending,
    bool IsProfileNotificationsEnabled,
    int FollowersCount,
    int FollowingCount,
    string? ErrorCode = null)
{
    public static UserFollowRelationResult Success(
        bool isFollowing,
        bool isFollowedBy,
        bool isFollowRequestPending,
        bool isProfileNotificationsEnabled,
        int followersCount,
        int followingCount) =>
        new(UserFollowListStatus.Success, isFollowing, isFollowedBy, isFollowRequestPending, isProfileNotificationsEnabled, followersCount, followingCount);

    public static UserFollowRelationResult Unauthorized(string? errorCode = null) =>
        new(UserFollowListStatus.Unauthorized, false, false, false, false, 0, 0, errorCode);

    public static UserFollowRelationResult ProvisioningPending(string? errorCode = null) =>
        new(UserFollowListStatus.ProvisioningPending, false, false, false, false, 0, 0, errorCode);

    public static UserFollowRelationResult NotFound(string? errorCode = null) =>
        new(UserFollowListStatus.NotFound, false, false, false, false, 0, 0, errorCode);

    public static UserFollowRelationResult ValidationFailed(string? errorCode = null) =>
        new(UserFollowListStatus.ValidationFailed, false, false, false, false, 0, 0, errorCode);
}
