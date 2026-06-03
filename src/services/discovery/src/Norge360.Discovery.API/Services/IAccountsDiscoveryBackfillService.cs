// <copyright file="IAccountsDiscoveryBackfillService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.API.Services;

public interface IAccountsDiscoveryBackfillService
{
    Task<DiscoveryBackfillResponse> BackfillAsync(int take, int maxBatches, CancellationToken cancellationToken = default);
}
