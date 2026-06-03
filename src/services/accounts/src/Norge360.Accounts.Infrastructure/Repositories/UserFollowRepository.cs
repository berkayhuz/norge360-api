// <copyright file="UserFollowRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class UserFollowRepository(AccountsDbContext dbContext) : IUserFollowRepository
{
    public Task<UserFollow?> GetAsync(Guid followerProfileId, Guid followeeProfileId, CancellationToken cancellationToken = default) =>
        dbContext.UserFollows.FirstOrDefaultAsync(
            follow => follow.FollowerId == followerProfileId && follow.FolloweeId == followeeProfileId,
            cancellationToken);

    public Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default) =>
        dbContext.UserFollows.AddAsync(follow, cancellationToken).AsTask();

    public void Remove(UserFollow follow) => dbContext.UserFollows.Remove(follow);
}
