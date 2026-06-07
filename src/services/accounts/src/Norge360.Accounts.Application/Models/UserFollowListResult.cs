// <copyright file="UserFollowListResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;

namespace Norge360.Accounts.Application.Models;

public enum UserFollowListStatus
{
    Success = 0,
    Unauthorized = 1,
    ProvisioningPending = 2,
    NotFound = 3,
    ValidationFailed = 4
}

public sealed record UserFollowListResult(
    UserFollowListStatus Status,
    int Page,
    int PageSize,
    IReadOnlyCollection<UserFollowListItem> Items,
    string? ErrorCode = null)
{
    public static UserFollowListResult Success(
        int page,
        int pageSize,
        IReadOnlyCollection<UserFollowListItem> items) =>
        new(UserFollowListStatus.Success, page, pageSize, items);

    public static UserFollowListResult Unauthorized(string? errorCode = null) =>
        new(UserFollowListStatus.Unauthorized, 1, 20, [], errorCode);

    public static UserFollowListResult ProvisioningPending(string? errorCode = null) =>
        new(UserFollowListStatus.ProvisioningPending, 1, 20, [], errorCode);

    public static UserFollowListResult NotFound(string? errorCode = null) =>
        new(UserFollowListStatus.NotFound, 1, 20, [], errorCode);

    public static UserFollowListResult ValidationFailed(string? errorCode = null) =>
        new(UserFollowListStatus.ValidationFailed, 1, 20, [], errorCode);
}
