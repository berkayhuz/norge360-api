// <copyright file="UsernameHistoryRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class UsernameHistoryRepository(AccountsDbContext dbContext) : IUsernameHistoryRepository
{
    public Task<bool> IsLockedByAnotherProfileAsync(
        string normalizedUsername,
        Guid? currentProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UsernameHistory
            .AsNoTracking()
            .AnyAsync(
                history =>
                    history.NormalizedOldUsername == normalizedUsername &&
                    history.ReleasedAt == null &&
                    (!currentProfileId.HasValue || history.ProfileId != currentProfileId.Value),
                cancellationToken);
}
