// <copyright file="UserBlockRelationsResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record UserBlockRelationsResult(
    UserBlockListStatus Status,
    IReadOnlyCollection<Guid> BlockedProfileIds,
    IReadOnlyCollection<Guid> BlockerProfileIds,
    string? ErrorCode = null)
{
    public static UserBlockRelationsResult Success(
        IReadOnlyCollection<Guid> blockedProfileIds,
        IReadOnlyCollection<Guid> blockerProfileIds) =>
        new(UserBlockListStatus.Success, blockedProfileIds, blockerProfileIds);

    public static UserBlockRelationsResult Unauthorized(string? errorCode = null) =>
        new(UserBlockListStatus.Unauthorized, [], [], errorCode);

    public static UserBlockRelationsResult ProvisioningPending(string? errorCode = null) =>
        new(UserBlockListStatus.ProvisioningPending, [], [], errorCode);
}
