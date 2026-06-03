// <copyright file="UserProfileRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class UserProfileRepository(AccountsDbContext dbContext) : IUserProfileRepository
{
    public Task<UserProfile?> GetByAuthUserIdAsync(
        Guid authUserId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        ReadProfiles(includeDeleted)
            .Where(profile => profile.AuthUserId == authUserId)
            .OrderBy(profile => profile.IsDeleted)
            .ThenByDescending(profile => profile.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserProfile?> GetTrackedByAuthUserIdAsync(
        Guid authUserId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        WriteProfiles(includeDeleted)
            .Where(profile => profile.AuthUserId == authUserId)
            .OrderBy(profile => profile.IsDeleted)
            .ThenByDescending(profile => profile.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserProfile?> GetByNormalizedUsernameAsync(
        string normalizedUsername,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        ReadProfiles(includeDeleted)
            .Where(profile => profile.NormalizedUsername == normalizedUsername)
            .OrderBy(profile => profile.IsDeleted)
            .ThenByDescending(profile => profile.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserProfile?> GetTrackedByNormalizedUsernameAsync(
        string normalizedUsername,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        WriteProfiles(includeDeleted)
            .Where(profile => profile.NormalizedUsername == normalizedUsername)
            .OrderBy(profile => profile.IsDeleted)
            .ThenByDescending(profile => profile.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserProfile?> GetByProfileIdAsync(
        Guid profileId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        ReadProfiles(includeDeleted)
            .Where(profile => profile.Id == profileId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsByAuthUserIdAsync(
        Guid authUserId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        ReadProfiles(includeDeleted)
            .AnyAsync(profile => profile.AuthUserId == authUserId, cancellationToken);

    public Task<bool> ExistsByNormalizedUsernameAsync(
        string normalizedUsername,
        Guid? excludingProfileId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(
                profile =>
                    !profile.IsDeleted &&
                    profile.NormalizedUsername == normalizedUsername &&
                    (!excludingProfileId.HasValue || profile.Id != excludingProfileId.Value),
                cancellationToken);

    public Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default) =>
        dbContext.UserProfiles.AddAsync(profile, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<Guid>> ListExistingProfileIdsAsync(
        IReadOnlyCollection<Guid> profileIds,
        CancellationToken cancellationToken = default)
    {
        if (profileIds.Count == 0)
        {
            return [];
        }

        return await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => !profile.IsDeleted && profileIds.Contains(profile.Id))
            .Select(profile => profile.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserProfile>> ListActiveProfilesForReindexBatchAsync(
        DateTime? lastCreatedAtUtc,
        Guid? lastProfileId,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 1, 500);
        var query = dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => !profile.IsDeleted && profile.IsActive);

        if (lastCreatedAtUtc.HasValue && lastProfileId.HasValue)
        {
            var createdAt = lastCreatedAtUtc.Value;
            var profileId = lastProfileId.Value;
            query = query.Where(profile =>
                profile.CreatedAt > createdAt ||
                (profile.CreatedAt == createdAt && profile.Id.CompareTo(profileId) > 0));
        }

        return await query
            .OrderBy(profile => profile.CreatedAt)
            .ThenBy(profile => profile.Id)
            .Take(safeBatchSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserProfile>> ListDiscoveryExportBatchAsync(
        DateTime? lastUpdatedAtUtc,
        Guid? lastProfileId,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 1, 500);
        var query = dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile =>
                !profile.IsDeleted &&
                profile.IsActive &&
                profile.ProfileVisibility == Domain.Enums.ProfileVisibility.Public);

        if (lastUpdatedAtUtc.HasValue && lastProfileId.HasValue)
        {
            var updatedAt = lastUpdatedAtUtc.Value;
            var profileId = lastProfileId.Value;
            query = query.Where(profile =>
                (profile.UpdatedAt ?? profile.CreatedAt) > updatedAt ||
                ((profile.UpdatedAt ?? profile.CreatedAt) == updatedAt && profile.Id.CompareTo(profileId) > 0));
        }

        return await query
            .OrderBy(profile => profile.UpdatedAt ?? profile.CreatedAt)
            .ThenBy(profile => profile.Id)
            .Take(safeBatchSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserProfile>> ListByAuthUserIdsAsync(
        IReadOnlyCollection<Guid> authUserIds,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (authUserIds.Count == 0)
        {
            return [];
        }

        return await ReadProfiles(includeDeleted)
            .Where(profile => authUserIds.Contains(profile.AuthUserId))
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<UserProfile> ReadProfiles(bool includeDeleted)
    {
        var profiles = includeDeleted
            ? dbContext.UserProfiles.IgnoreQueryFilters()
            : dbContext.UserProfiles.Where(profile => !profile.IsDeleted);

        return profiles.AsNoTracking();
    }

    private IQueryable<UserProfile> WriteProfiles(bool includeDeleted) =>
        includeDeleted
            ? dbContext.UserProfiles.IgnoreQueryFilters()
            : dbContext.UserProfiles.Where(profile => !profile.IsDeleted);
}
