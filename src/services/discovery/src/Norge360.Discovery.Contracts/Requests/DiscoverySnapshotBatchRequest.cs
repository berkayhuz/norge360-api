// <copyright file="DiscoverySnapshotBatchRequest.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Discovery.Contracts.Requests;

public sealed record DiscoverySnapshotBatchRequest(IReadOnlyList<DiscoverySnapshotUpsertRequest> Snapshots);

public sealed record DiscoverySnapshotUpsertRequest(
    Guid ProfileId,
    Guid? AuthUserId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool IsVerified,
    string Visibility,
    bool IsActive,
    bool IsDeleted,
    int FollowersCount,
    int PostsCount,
    DateTimeOffset UpdatedAt);
