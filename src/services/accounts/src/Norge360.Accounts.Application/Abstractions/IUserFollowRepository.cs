// <copyright file="IUserFollowRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserFollowRepository
{
    Task<UserFollow?> GetAsync(Guid followerProfileId, Guid followeeProfileId, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveAsync(Guid followerProfileId, Guid followeeProfileId, CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingAsync(Guid followerProfileId, Guid followeeProfileId, CancellationToken cancellationToken = default);

    Task<int> CountFollowersAsync(Guid followeeProfileId, CancellationToken cancellationToken = default);

    Task<int> CountFollowingAsync(Guid followerProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> ListFollowingProfileIdsAsync(
        Guid followerProfileId,
        IReadOnlyCollection<Guid> followeeProfileIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> ListFollowerAuthUserIdsAsync(
        Guid followeeProfileId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserFollowListItem>> ListFollowersAsync(
        Guid followeeProfileId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserFollowListItem>> ListFollowingAsync(
        Guid followerProfileId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default);

    void Remove(UserFollow follow);
}

public sealed record UserFollowListItem(
    Guid ProfileId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    DateTimeOffset FollowedAtUtc);
