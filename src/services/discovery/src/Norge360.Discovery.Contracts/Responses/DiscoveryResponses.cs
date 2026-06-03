namespace Norge360.Discovery.Contracts.Responses;

public sealed record DiscoveryEventIngestionResponse(int Accepted, int Duplicates, int Invalid);

public sealed record DiscoverUserResponse(
    Guid? UserId,
    Guid ProfileId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool IsVerified,
    bool ViewerFollowsThisUser,
    string ReasonLabel);

public sealed record DiscoveryHubResponse(
    IReadOnlyList<DiscoverUserResponse> PopularUsers,
    IReadOnlyList<DiscoverUserResponse> TrendingUsers,
    IReadOnlyList<DiscoverUserResponse> SuggestedUsers);
