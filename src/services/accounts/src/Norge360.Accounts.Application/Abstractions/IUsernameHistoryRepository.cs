// <copyright file="IUsernameHistoryRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Abstractions;

public interface IUsernameHistoryRepository
{
    Task<bool> IsLockedByAnotherProfileAsync(
        string normalizedUsername,
        Guid? currentProfileId,
        CancellationToken cancellationToken = default);
}
