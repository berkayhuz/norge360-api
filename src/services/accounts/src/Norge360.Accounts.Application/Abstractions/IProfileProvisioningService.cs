// <copyright file="IProfileProvisioningService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.IntegrationEvents.V1;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IProfileProvisioningService
{
    Task<UserProfile> ProvisionAsync(
        UserRegisteredV1 message,
        CancellationToken cancellationToken = default);
}
