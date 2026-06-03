// <copyright file="IUserBlockService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Models;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserBlockService
{
    Task<UserBlockMutationResult> BlockByUsernameAsync(
        Guid blockerAuthUserId,
        string blockedUsername,
        CancellationToken cancellationToken = default);

    Task<UserBlockMutationResult> UnblockByUsernameAsync(
        Guid blockerAuthUserId,
        string blockedUsername,
        CancellationToken cancellationToken = default);

    Task<UserBlockListResult> ListBlockedAsync(
        Guid blockerAuthUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UserBlockRelationsResult> ListBlockRelationsAsync(
        Guid requesterAuthUserId,
        CancellationToken cancellationToken = default);
}
