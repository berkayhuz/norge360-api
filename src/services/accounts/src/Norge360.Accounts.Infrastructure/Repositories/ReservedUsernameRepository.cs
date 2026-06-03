// <copyright file="ReservedUsernameRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class ReservedUsernameRepository(AccountsDbContext dbContext) : IReservedUsernameRepository
{
    public Task<bool> IsReservedAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default) =>
        dbContext.ReservedUsernames
            .AsNoTracking()
            .AnyAsync(
                reservedUsername =>
                    reservedUsername.IsActive &&
                    reservedUsername.NormalizedValue == normalizedUsername,
                cancellationToken);
}
