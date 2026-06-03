// <copyright file="FollowAccessChecker.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Enums;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Services;

public sealed class FollowAccessChecker(AccountsDbContext dbContext) : IFollowAccessChecker
{
    public Task<bool> IsActiveFollowerAsync(
        Guid followerProfileId,
        Guid followeeProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserFollows
            .AsNoTracking()
            .AnyAsync(
                follow =>
                    follow.FollowerId == followerProfileId &&
                    follow.FolloweeId == followeeProfileId &&
                    follow.Status == FollowStatus.Active,
                cancellationToken);
}
