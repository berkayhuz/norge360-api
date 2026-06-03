namespace Norge360.Accounts.Contracts.Responses;

public sealed record DiscoveryProfileExportItem(
    Guid ProfileId,
    Guid AuthUserId,
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

public sealed record DiscoveryProfileExportResponse(
    IReadOnlyList<DiscoveryProfileExportItem> Items,
    DateTimeOffset? NextUpdatedAt,
    Guid? NextProfileId,
    bool HasMore);
