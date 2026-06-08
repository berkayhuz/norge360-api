// <copyright file="UserProfileReportRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class UserProfileReportRepository(AccountsDbContext dbContext) : IUserProfileReportRepository
{
    public Task AddAsync(UserProfileReport report, CancellationToken cancellationToken = default) =>
        dbContext.UserProfileReports.AddAsync(report, cancellationToken).AsTask();
}
