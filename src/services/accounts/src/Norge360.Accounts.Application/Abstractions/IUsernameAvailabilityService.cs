// <copyright file="IUsernameAvailabilityService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUsernameAvailabilityService
{
    Task<UsernameAvailabilityResponse> CheckAsync(
        string? username,
        Guid? excludingProfileId = null,
        CancellationToken cancellationToken = default);
}
