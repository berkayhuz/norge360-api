// <copyright file="IDiscoveryRankingService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.Application.Abstractions;

public interface IDiscoveryRankingService
{
    Task RecomputeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscoverUserResponse>> GetPopularUsersAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscoverUserResponse>> GetTrendingUsersAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscoverUserResponse>> GetFollowSuggestionsAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default);
    Task<DiscoveryHubResponse> GetHubAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default);
}
