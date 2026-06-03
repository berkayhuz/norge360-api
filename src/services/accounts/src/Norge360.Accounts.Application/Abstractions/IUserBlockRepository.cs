// <copyright file="IUserBlockRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserBlockRepository
{
    Task<UserBlock?> GetAsync(
        Guid blockerProfileId,
        Guid blockedProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserBlockListItem>> ListBlockedAsync(
        Guid blockerProfileId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> ListBlockedProfileIdsAsync(
        Guid blockerProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> ListBlockerProfileIdsAsync(
        Guid blockedProfileId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBetweenAsync(
        Guid firstProfileId,
        Guid secondProfileId,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserBlock block, CancellationToken cancellationToken = default);

    void Remove(UserBlock block);
}

public sealed record UserBlockListItem(
    Guid BlockedProfileId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    DateTimeOffset BlockedAtUtc);
