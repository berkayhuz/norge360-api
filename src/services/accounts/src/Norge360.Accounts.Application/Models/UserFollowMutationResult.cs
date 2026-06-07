// <copyright file="UserFollowMutationResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record UserFollowMutationResult(
    UserFollowMutationStatus Status,
    bool IsFollowing = false,
    bool IsFollowRequestPending = false,
    int FollowersCount = 0,
    int FollowingCount = 0,
    string? ErrorCode = null)
{
    public static UserFollowMutationResult Success(
        bool isFollowing = true,
        bool isFollowRequestPending = false,
        int followersCount = 0,
        int followingCount = 0) =>
        new(UserFollowMutationStatus.Success, isFollowing, isFollowRequestPending, followersCount, followingCount);

    public static UserFollowMutationResult Pending(
        int followersCount,
        int followingCount) =>
        new(UserFollowMutationStatus.Success, false, true, followersCount, followingCount);

    public static UserFollowMutationResult Unauthorized(string errorCode) => new(UserFollowMutationStatus.Unauthorized, ErrorCode: errorCode);

    public static UserFollowMutationResult ValidationFailed(string? errorCode) => new(UserFollowMutationStatus.ValidationFailed, ErrorCode: errorCode);

    public static UserFollowMutationResult NotFound(string errorCode) => new(UserFollowMutationStatus.NotFound, ErrorCode: errorCode);

    public static UserFollowMutationResult ProvisioningPending(string errorCode) => new(UserFollowMutationStatus.ProvisioningPending, ErrorCode: errorCode);
}

public enum UserFollowMutationStatus
{
    Success = 0,
    Unauthorized = 1,
    ValidationFailed = 2,
    NotFound = 3,
    ProvisioningPending = 4
}
