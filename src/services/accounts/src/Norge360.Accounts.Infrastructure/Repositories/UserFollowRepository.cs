// <copyright file="UserFollowRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class UserFollowRepository(AccountsDbContext dbContext) : IUserFollowRepository
{
    public Task<UserFollow?> GetAsync(Guid followerProfileId, Guid followeeProfileId, CancellationToken cancellationToken = default) =>
        dbContext.UserFollows.FirstOrDefaultAsync(
            follow => follow.FollowerId == followerProfileId && follow.FolloweeId == followeeProfileId,
            cancellationToken);

    public Task<bool> ExistsActiveAsync(
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

    public Task<bool> ExistsPendingAsync(
        Guid followerProfileId,
        Guid followeeProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserFollows
            .AsNoTracking()
            .AnyAsync(
                follow =>
                    follow.FollowerId == followerProfileId &&
                    follow.FolloweeId == followeeProfileId &&
                    follow.Status == FollowStatus.Pending,
                cancellationToken);

    public Task<int> CountFollowersAsync(
        Guid followeeProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserFollows
            .AsNoTracking()
            .CountAsync(
                follow =>
                    follow.FolloweeId == followeeProfileId &&
                    follow.Status == FollowStatus.Active,
                cancellationToken);

    public Task<int> CountFollowingAsync(
        Guid followerProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserFollows
            .AsNoTracking()
            .CountAsync(
                follow =>
                    follow.FollowerId == followerProfileId &&
                    follow.Status == FollowStatus.Active,
                cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> ListFollowingProfileIdsAsync(
        Guid followerProfileId,
        IReadOnlyCollection<Guid> followeeProfileIds,
        CancellationToken cancellationToken = default)
    {
        if (followeeProfileIds.Count == 0)
        {
            return [];
        }

        return await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => follow.FollowerId == followerProfileId && follow.Status == FollowStatus.Active && followeeProfileIds.Contains(follow.FolloweeId))
            .Select(follow => follow.FolloweeId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> ListFollowerAuthUserIdsAsync(
        Guid followeeProfileId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 1000);

        return await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => follow.FolloweeId == followeeProfileId && follow.Status == FollowStatus.Active)
            .Join(
                dbContext.UserProfiles.AsNoTracking().Where(profile => !profile.IsDeleted && profile.IsActive),
                follow => follow.FollowerId,
                profile => profile.Id,
                (_, profile) => profile.AuthUserId)
            .Distinct()
            .Take(safeLimit)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserFollowListItem>> ListFollowersAsync(
        Guid followeeProfileId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var safeOffset = Math.Max(offset, 0);

        return await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => follow.FolloweeId == followeeProfileId && follow.Status == FollowStatus.Active)
            .Join(
                dbContext.UserProfiles.AsNoTracking().Where(profile => !profile.IsDeleted && profile.IsActive),
                follow => follow.FollowerId,
                profile => profile.Id,
                (follow, profile) => new
                {
                    profile.AvatarUrl,
                    profile.DisplayName,
                    profile.Id,
                    profile.Username,
                    follow.CreatedAt
                })
            .OrderByDescending(item => item.CreatedAt)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(item => new UserFollowListItem(
                item.Id,
                item.Username,
                item.DisplayName,
                item.AvatarUrl,
                item.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserFollowListItem>> ListFollowingAsync(
        Guid followerProfileId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var safeOffset = Math.Max(offset, 0);

        return await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => follow.FollowerId == followerProfileId && follow.Status == FollowStatus.Active)
            .Join(
                dbContext.UserProfiles.AsNoTracking().Where(profile => !profile.IsDeleted && profile.IsActive),
                follow => follow.FolloweeId,
                profile => profile.Id,
                (follow, profile) => new
                {
                    profile.AvatarUrl,
                    profile.DisplayName,
                    profile.Id,
                    profile.Username,
                    follow.CreatedAt
                })
            .OrderByDescending(item => item.CreatedAt)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(item => new UserFollowListItem(
                item.Id,
                item.Username,
                item.DisplayName,
                item.AvatarUrl,
                item.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default) =>
        dbContext.UserFollows.AddAsync(follow, cancellationToken).AsTask();

    public void Remove(UserFollow follow) => dbContext.UserFollows.Remove(follow);
}
