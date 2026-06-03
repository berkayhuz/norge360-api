// <copyright file="UserFollowMutationResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record UserFollowMutationResult(UserFollowMutationStatus Status, string? ErrorCode = null)
{
    public static UserFollowMutationResult Success() => new(UserFollowMutationStatus.Success);

    public static UserFollowMutationResult Unauthorized(string errorCode) => new(UserFollowMutationStatus.Unauthorized, errorCode);

    public static UserFollowMutationResult ValidationFailed(string? errorCode) => new(UserFollowMutationStatus.ValidationFailed, errorCode);

    public static UserFollowMutationResult NotFound(string errorCode) => new(UserFollowMutationStatus.NotFound, errorCode);

    public static UserFollowMutationResult ProvisioningPending(string errorCode) => new(UserFollowMutationStatus.ProvisioningPending, errorCode);
}

public enum UserFollowMutationStatus
{
    Success = 0,
    Unauthorized = 1,
    ValidationFailed = 2,
    NotFound = 3,
    ProvisioningPending = 4
}
