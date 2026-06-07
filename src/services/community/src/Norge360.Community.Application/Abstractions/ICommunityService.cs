// <copyright file="ICommunityService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Contracts.Requests;
using Norge360.Community.Contracts.Responses;

namespace Norge360.Community.Application.Abstractions;

public interface ICommunityService
{
    Task<CommunityPostDto> CreatePostAsync(Guid userId, CreateCommunityPostRequest request, CancellationToken cancellationToken);
    Task<CommunityPostDto?> GetPostAsync(Guid postId, Guid? currentUserId, CancellationToken cancellationToken);
    Task<CommunityPostDto?> GetPostBySlugAsync(string username, string postSlug, Guid? currentUserId, CancellationToken cancellationToken);
    Task<PagedCommunityFeedResponse> GetFeedAsync(int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken);
    Task<PagedCommunityFeedResponse> GetUserPostsAsync(Guid userId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken);
    Task<CommunityPostDto?> UpdatePostAsync(Guid postId, Guid actorUserId, bool isModerator, UpdateCommunityPostRequest request, CancellationToken cancellationToken);
    Task<bool> DeletePostAsync(Guid postId, Guid actorUserId, bool isModerator, CancellationToken cancellationToken);
    Task<CommunityPostDto?> SetPostCommentsEnabledAsync(Guid postId, Guid actorUserId, bool isModerator, bool enabled, CancellationToken cancellationToken);
    Task<CommunityPostDto?> SetPostHideLikeCountAsync(Guid postId, Guid actorUserId, bool isModerator, bool hideLikeCount, CancellationToken cancellationToken);
    Task<CommunityCommentDto?> GetCommentAsync(Guid commentId, Guid? currentUserId, CancellationToken cancellationToken);
    Task<CommunityCommentDto?> GetCommentBySlugAsync(string username, string postSlug, string commentSlug, Guid? currentUserId, CancellationToken cancellationToken);
    Task<PagedCommunityCommentsResponse?> GetPostCommentsAsync(Guid postId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken);
    Task<PagedCommunityCommentsResponse?> GetPostCommentsBySlugAsync(string username, string postSlug, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken);
    Task<PagedCommunityCommentsResponse?> GetCommentRepliesAsync(Guid commentId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken);
    Task<PagedCommunityCommentsResponse?> GetCommentRepliesBySlugAsync(string username, string postSlug, string commentSlug, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken);
    Task<CommunityCommentDto?> AddCommentAsync(Guid postId, Guid userId, CreateCommunityCommentRequest request, CancellationToken cancellationToken);
    Task<CommunityCommentDto?> ReplyCommentAsync(Guid commentId, Guid userId, CreateCommunityReplyRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid actorUserId, bool isModerator, CancellationToken cancellationToken);
    Task<ToggleActionResponse?> SetPostLikeAsync(Guid postId, Guid userId, bool liked, CancellationToken cancellationToken);
    Task<ToggleActionResponse?> SetCommentLikeAsync(Guid commentId, Guid userId, bool liked, CancellationToken cancellationToken);
    Task<ToggleActionResponse?> SetSavedPostAsync(Guid postId, Guid userId, bool saved, CancellationToken cancellationToken);
    Task<string?> SetPostInterestAsync(Guid postId, Guid userId, SetCommunityPostInterestRequest request, CancellationToken cancellationToken);
    Task<bool> ClearPostInterestAsync(Guid postId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityReactionSummaryDto>?> SetPostReactionAsync(Guid postId, Guid userId, AddOrUpdateCommunityReactionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityReactionSummaryDto>?> RemovePostReactionAsync(Guid postId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityReactionSummaryDto>?> SetCommentReactionAsync(Guid commentId, Guid userId, AddOrUpdateCommunityReactionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityReactionSummaryDto>?> RemoveCommentReactionAsync(Guid commentId, Guid userId, CancellationToken cancellationToken);
    Task<bool?> ReportPostAsync(Guid postId, Guid reporterUserId, ReportCommunityPostRequest request, CancellationToken cancellationToken);
    Task<bool?> ReportCommentAsync(Guid commentId, Guid reporterUserId, ReportCommunityCommentRequest request, CancellationToken cancellationToken);
}
