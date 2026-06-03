namespace Norge360.Community.Application.Models;

public sealed record CommunityAuthorSummary(
    Guid UserId,
    string? Username,
    string? DisplayName,
    string? AvatarUrl,
    bool IsVerified,
    bool CanViewPosts);
