// <copyright file="IUserProfileRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByAuthUserIdAsync(
        Guid authUserId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetTrackedByAuthUserIdAsync(
        Guid authUserId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetByNormalizedUsernameAsync(
        string normalizedUsername,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetTrackedByNormalizedUsernameAsync(
        string normalizedUsername,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetByProfileIdAsync(
        Guid profileId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByAuthUserIdAsync(
        Guid authUserId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedUsernameAsync(
        string normalizedUsername,
        Guid? excludingProfileId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> ListExistingProfileIdsAsync(
        IReadOnlyCollection<Guid> profileIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserProfile>> ListActiveProfilesForReindexBatchAsync(
        DateTime? lastCreatedAtUtc,
        Guid? lastProfileId,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserProfile>> ListDiscoveryExportBatchAsync(
        DateTime? lastUpdatedAtUtc,
        Guid? lastProfileId,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserProfile>> ListByAuthUserIdsAsync(
        IReadOnlyCollection<Guid> authUserIds,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
