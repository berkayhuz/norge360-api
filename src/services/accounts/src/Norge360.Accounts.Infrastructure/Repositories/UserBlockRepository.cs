// <copyright file="UserBlockRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class UserBlockRepository(AccountsDbContext dbContext) : IUserBlockRepository
{
    public Task<UserBlock?> GetAsync(
        Guid blockerProfileId,
        Guid blockedProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserBlocks
            .FirstOrDefaultAsync(
                block => block.BlockerProfileId == blockerProfileId && block.BlockedProfileId == blockedProfileId,
                cancellationToken);

    public async Task<IReadOnlyCollection<UserBlockListItem>> ListBlockedAsync(
        Guid blockerProfileId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var safeOffset = Math.Max(offset, 0);

        return await dbContext.UserBlocks
            .AsNoTracking()
            .Where(block => block.BlockerProfileId == blockerProfileId)
            .Join(
                dbContext.UserProfiles.AsNoTracking().Where(profile => !profile.IsDeleted),
                block => block.BlockedProfileId,
                profile => profile.Id,
                (block, profile) => new UserBlockListItem(
                    profile.Id,
                    profile.Username,
                    profile.DisplayName,
                    profile.AvatarUrl,
                    block.CreatedAt))
            .OrderByDescending(item => item.BlockedAtUtc)
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(UserBlock block, CancellationToken cancellationToken = default) =>
        dbContext.UserBlocks.AddAsync(block, cancellationToken).AsTask();

    public void Remove(UserBlock block) => dbContext.UserBlocks.Remove(block);

    public async Task<IReadOnlyCollection<Guid>> ListBlockedProfileIdsAsync(
        Guid blockerProfileId,
        CancellationToken cancellationToken = default) =>
        await dbContext.UserBlocks
            .AsNoTracking()
            .Where(block => block.BlockerProfileId == blockerProfileId)
            .Select(block => block.BlockedProfileId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> ListBlockerProfileIdsAsync(
        Guid blockedProfileId,
        CancellationToken cancellationToken = default) =>
        await dbContext.UserBlocks
            .AsNoTracking()
            .Where(block => block.BlockedProfileId == blockedProfileId)
            .Select(block => block.BlockerProfileId)
            .ToArrayAsync(cancellationToken);

    public Task<bool> ExistsBetweenAsync(
        Guid firstProfileId,
        Guid secondProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(
                block =>
                    (block.BlockerProfileId == firstProfileId && block.BlockedProfileId == secondProfileId) ||
                    (block.BlockerProfileId == secondProfileId && block.BlockedProfileId == firstProfileId),
                cancellationToken);
}
