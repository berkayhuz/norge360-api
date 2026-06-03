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
