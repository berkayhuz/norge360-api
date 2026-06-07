// <copyright file="ProfileResponse.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Contracts.Responses;

public sealed record ProfileResponse(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? CoverPhotoUrl,
    string? Country,
    string? City,
    string? District,
    string? Occupation,
    string? Company,
    string? Website,
    int? FollowersCount,
    int? FollowingCount,
    int? PostsCount,
    bool IsVerified,
    string AccountType,
    string ProfileVisibility,
    string CommentAudience,
    bool HideLikeCounts,
    DateTimeOffset? LastSeenAt,
    DateTime? CreatedAt,
    bool? IsFollowedByCurrentUser,
    bool? IsFollowingCurrentUser,
    bool? IsFollowRequestPending,
    bool? IsProfileNotificationsEnabled);
