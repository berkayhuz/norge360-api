// <copyright file="IUserFollowRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserFollowRepository
{
    Task<UserFollow?> GetAsync(Guid followerProfileId, Guid followeeProfileId, CancellationToken cancellationToken = default);

    Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default);

    void Remove(UserFollow follow);
}
