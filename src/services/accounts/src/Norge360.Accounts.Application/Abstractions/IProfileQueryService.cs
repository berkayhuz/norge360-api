// <copyright file="IProfileQueryService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Abstractions;

public interface IProfileQueryService
{
    Task<ProfileQueryResult<MyProfileResponse>> GetMyProfileAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default);

    Task<ProfileQueryResult<ProfileResponse>> GetPublicProfileByUsernameAsync(
        string username,
        Guid? viewerAuthUserId = null,
        CancellationToken cancellationToken = default);

    Task<Guid?> ResolveAuthUserIdByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveUsernameByAuthUserIdAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default);

    Task<Guid?> ResolveAuthUserIdByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<InternalUserBatchSummaryResponse> GetInternalUserBatchSummaryAsync(
        InternalUserBatchSummaryRequest request,
        Guid? viewerAuthUserId,
        CancellationToken cancellationToken = default);

    Task<DiscoveryProfileExportResponse> GetDiscoveryProfileExportBatchAsync(
        DateTimeOffset? lastUpdatedAt,
        Guid? lastProfileId,
        int take,
        CancellationToken cancellationToken = default);
}
