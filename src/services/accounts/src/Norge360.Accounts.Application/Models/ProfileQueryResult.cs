// <copyright file="ProfileQueryResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record ProfileQueryResult<T>(
    ProfileQueryStatus Status,
    T? Value,
    string? Reason)
{
    public static ProfileQueryResult<T> Success(T value) => new(
        ProfileQueryStatus.Success,
        value,
        null);

    public static ProfileQueryResult<T> NotFound(string? reason = null) => new(
        ProfileQueryStatus.NotFound,
        default,
        reason);

    public static ProfileQueryResult<T> ProvisioningPending(string? reason = null) => new(
        ProfileQueryStatus.ProvisioningPending,
        default,
        reason);

    public static ProfileQueryResult<T> Unauthorized(string? reason = null) => new(
        ProfileQueryStatus.Unauthorized,
        default,
        reason);
}
