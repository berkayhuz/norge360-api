// <copyright file="UserBlockListResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;

namespace Norge360.Accounts.Application.Models;

public enum UserBlockListStatus
{
    Success = 0,
    Unauthorized = 1,
    ProvisioningPending = 2
}

public sealed record UserBlockListResult(
    UserBlockListStatus Status,
    int Page,
    int PageSize,
    IReadOnlyCollection<UserBlockListItem> Items,
    string? ErrorCode = null)
{
    public static UserBlockListResult Success(
        int page,
        int pageSize,
        IReadOnlyCollection<UserBlockListItem> items) =>
        new(UserBlockListStatus.Success, page, pageSize, items);

    public static UserBlockListResult Unauthorized(string? errorCode = null) =>
        new(UserBlockListStatus.Unauthorized, 1, 20, [], errorCode);

    public static UserBlockListResult ProvisioningPending(string? errorCode = null) =>
        new(UserBlockListStatus.ProvisioningPending, 1, 20, [], errorCode);
}
