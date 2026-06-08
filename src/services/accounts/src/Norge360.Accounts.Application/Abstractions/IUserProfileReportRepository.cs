// <copyright file="IUserProfileReportRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUserProfileReportRepository
{
    Task AddAsync(UserProfileReport report, CancellationToken cancellationToken = default);
}
