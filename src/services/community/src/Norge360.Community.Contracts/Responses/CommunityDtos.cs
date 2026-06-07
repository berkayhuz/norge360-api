// <copyright file="CommunityDtos.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Community.Contracts.Responses;

public sealed record CommunityAuthorDto(
    Guid UserId,
    string? Username,
    string? DisplayName,
    string? AvatarUrl,
    bool IsVerified,
    bool HideLikeCounts);
public sealed record CommunityPostMediaDto(Guid Id, string PublicUrl, string ContentType, long SizeBytes, int Width, int Height, short Order, string Status);
public sealed record CommunityReactionSummaryDto(string EmojiCode, string Emoji, int Count);
public sealed record CommunityCommentDto(
    Guid Id,
    string Slug,
    Guid PostId,
    Guid UserId,
    Guid? ParentCommentId,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsLikedByCurrentUser,
    string? CurrentUserReaction,
    int LikesCount,
    IReadOnlyList<CommunityReactionSummaryDto> Reactions,
    int ReplyCount,
    CommunityCommentDto? PinnedReply,
    bool CanDelete,
    bool CanReport);

public sealed record CommunityPostDto(
    Guid Id,
    string Slug,
    Guid UserId,
    string? Caption,
    string? City,
    string? District,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool CommentsEnabled,
    bool? HideLikeCountOverride,
    int CommentsCount,
    int LikesCount,
    int SavesCount,
    bool IsLikedByCurrentUser,
    bool IsSavedByCurrentUser,
    string? CurrentUserReaction,
    string? CurrentUserInterest,
    bool CanEdit,
    bool CanDelete,
    bool CanReport,
    CommunityAuthorDto? Author,
    IReadOnlyList<CommunityPostMediaDto> Media);

public sealed record CommunityFeedItemDto(CommunityPostDto Post, IReadOnlyList<CommunityReactionSummaryDto> ReactionSummary);
public sealed record PagedCommunityFeedResponse(IReadOnlyList<CommunityFeedItemDto> Items, int Page, int PageSize, int TotalCount, bool HasNextPage);
public sealed record PagedCommunityCommentsResponse(IReadOnlyList<CommunityCommentDto> Items, int Page, int PageSize, int TotalCount);
public sealed record ToggleActionResponse(bool Active, int Count);
