// <copyright file="IUserFollowService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Models;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserFollowService
{
    Task<UserFollowMutationResult> FollowByUsernameAsync(Guid followerAuthUserId, string followeeUsername, CancellationToken cancellationToken = default);

    Task<UserFollowMutationResult> UnfollowByUsernameAsync(Guid followerAuthUserId, string followeeUsername, CancellationToken cancellationToken = default);
}
