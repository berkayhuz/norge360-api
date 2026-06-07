// <copyright file="CommunityService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;
using Norge360.Community.Contracts.Requests;
using Norge360.Community.Contracts.Responses;
using Norge360.Community.Domain.Entities;
using Norge360.Community.Domain.Enums;
using Norge360.Community.Domain.Utilities;

namespace Norge360.Community.Application.Services;

public sealed class CommunityService(
    ICommunityDbContext dbContext,
    ICommunityAuthorProfileProvider authorProfileProvider,
    ICommunityVisibilityService visibilityService,
    IDiscoveryEventPublisher discoveryEventPublisher,
    ICommunityNotificationPublisher notificationPublisher) : ICommunityService
{
    public async Task<CommunityPostDto> CreatePostAsync(Guid userId, CreateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        ValidateCaption(request.Caption);
        var post = new CommunityPost
        {
            UserId = userId,
            Slug = await GenerateUniquePostSlugAsync(cancellationToken),
            Caption = NormalizeNullable(request.Caption),
            City = NormalizeNullable(request.City),
            District = NormalizeNullable(request.District),
            Status = CommunityPostStatus.Published,
            CommentsEnabled = true
        };
        dbContext.CommunityPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        var postCount = await dbContext.CommunityPosts.CountAsync(x => x.UserId == userId && x.Status == CommunityPostStatus.Published, cancellationToken);
        await PublishCommunityNotificationAsync(
            async () => await notificationPublisher.PublishPostCreatedAsync(
                new CommunityPostPublishedNotification(
                    post.Id,
                    post.Slug,
                    post.Caption,
                    post.City,
                    post.CreatedAt == DateTime.MinValue ? DateTime.UtcNow : post.CreatedAt,
                    postCount == 1,
                    await ResolveNotificationActorAsync(userId, cancellationToken)),
                cancellationToken));
        return await BuildPostDto(post.Id, userId, cancellationToken) ?? throw new InvalidOperationException("post_create_failed");
    }

    public Task<CommunityPostDto?> GetPostAsync(Guid postId, Guid? currentUserId, CancellationToken cancellationToken)
        => BuildPostDto(postId, currentUserId, cancellationToken);

    public async Task<CommunityPostDto?> GetPostBySlugAsync(string username, string postSlug, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts
            .Where(x => x.Slug == postSlug && x.Status == CommunityPostStatus.Published)
            .Select(x => new { x.Id, x.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null && PublicSlugGenerator.TryDecodePublicGuidSlug(postSlug, out var decodedPostId))
        {
            post = await dbContext.CommunityPosts
                .Where(x => x.Id == decodedPostId && x.Status == CommunityPostStatus.Published)
                .Select(x => new { x.Id, x.UserId })
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (post is null)
        {
            return null;
        }

        var result = await BuildPostDto(post.Id, currentUserId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        return await IsPostOwnerUsernameAsync(post.UserId, username, cancellationToken) ? result : null;
    }

    public async Task<CommunityCommentDto?> GetCommentAsync(Guid commentId, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var comment = await dbContext.CommunityComments
            .Where(x => x.Id == commentId && x.Post.Status == CommunityPostStatus.Published)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return comment == Guid.Empty
            ? null
            : await BuildCommentDtoAsync(comment, currentUserId, cancellationToken);
    }

    public async Task<CommunityCommentDto?> GetCommentBySlugAsync(string username, string postSlug, string commentSlug, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var post = await ResolvePostForSlugAsync(postSlug, cancellationToken);
        if (post is null || !await IsPostOwnerUsernameAsync(post.UserId, username, cancellationToken))
        {
            return null;
        }

        var comment = await dbContext.CommunityComments
            .Where(x => x.Slug == commentSlug && x.PostId == post.Id && x.Post.Status == CommunityPostStatus.Published)
            .Select(x => new CommentSlugResolution(x.Id, x.PostId, x.Post.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (comment is null && PublicSlugGenerator.TryDecodePublicGuidSlug(commentSlug, out var decodedCommentId))
        {
            comment = await dbContext.CommunityComments
                .Where(x => x.Id == decodedCommentId && x.PostId == post.Id && x.Post.Status == CommunityPostStatus.Published)
                .Select(x => new CommentSlugResolution(x.Id, x.PostId, x.Post.UserId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (comment is null)
        {
            return null;
        }

        var result = await BuildCommentDtoAsync(comment.Id, currentUserId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        return result;
    }

    public async Task<PagedCommunityFeedResponse> GetFeedAsync(int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var blockedIds = currentUserId.HasValue
            ? await dbContext.CommunityPostInterests.Where(x => x.UserId == currentUserId.Value && x.InterestType == CommunityPostInterestType.NotInterested).Select(x => x.PostId).ToListAsync(cancellationToken)
            : [];

        var baseQuery = dbContext.CommunityPosts
            .AsNoTracking()
            .Where(x => x.Status == CommunityPostStatus.Published && !blockedIds.Contains(x.Id));

        var selected = await LoadVisibleFeedCandidatesAsync(baseQuery, page, pageSize, currentUserId, cancellationToken);
        if (selected.Count == 0)
        {
            return new PagedCommunityFeedResponse([], page, pageSize, 0, false);
        }

        var pageOffset = (page - 1) * pageSize;
        var pageSelected = selected.Skip(pageOffset).Take(pageSize).ToArray();
        var selectedIds = pageSelected.Select(x => x.Id).ToArray();
        var selectedAuthorIds = pageSelected.Select(x => x.UserId).Distinct().ToArray();
        var authorSummaries = await authorProfileProvider.GetAuthorSummariesAsync(selectedAuthorIds, currentUserId, cancellationToken);
        var items = await BuildFeedItemsAsync(selectedIds, currentUserId, authorSummaries, cancellationToken);
        var hasMoreVisible = selected.Count > page * pageSize;
        var total = hasMoreVisible
            ? (page * pageSize) + 1
            : ((page - 1) * pageSize) + items.Count;
        return new PagedCommunityFeedResponse(items, page, pageSize, total, hasMoreVisible);
    }

    public async Task<PagedCommunityFeedResponse> GetUserPostsAsync(Guid userId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        if (!await visibilityService.CanViewAuthorPostsAsync(userId, currentUserId, cancellationToken))
        {
            return new PagedCommunityFeedResponse([], page, pageSize, 0, false);
        }

        var query = dbContext.CommunityPosts.Where(x => x.UserId == userId && x.Status == CommunityPostStatus.Published);
        var total = await query.CountAsync(cancellationToken);
        var ids = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(cancellationToken);
        var authorSummaries = await authorProfileProvider.GetAuthorSummariesAsync([userId], currentUserId, cancellationToken);
        var items = await BuildFeedItemsAsync(ids, currentUserId, authorSummaries, cancellationToken);
        return new PagedCommunityFeedResponse(items, page, pageSize, total, page * pageSize < total);
    }

    public async Task<CommunityPostDto?> UpdatePostAsync(Guid postId, Guid actorUserId, bool isModerator, UpdateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        ValidateCaption(request.Caption);
        var post = await dbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return null;
        if (post.UserId != actorUserId && !isModerator) throw new UnauthorizedAccessException("post_update_forbidden");
        post.Caption = NormalizeNullable(request.Caption);
        post.City = NormalizeNullable(request.City);
        post.District = NormalizeNullable(request.District);
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPostDto(postId, actorUserId, cancellationToken);
    }

    public async Task<bool> DeletePostAsync(Guid postId, Guid actorUserId, bool isModerator, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return false;
        if (post.UserId != actorUserId && !isModerator) throw new UnauthorizedAccessException("post_delete_forbidden");
        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CommunityPostDto?> SetPostCommentsEnabledAsync(Guid postId, Guid actorUserId, bool isModerator, bool enabled, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return null;
        if (post.UserId != actorUserId && !isModerator) throw new UnauthorizedAccessException("post_update_forbidden");
        post.CommentsEnabled = enabled;
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPostDto(postId, actorUserId, cancellationToken);
    }

    public async Task<CommunityPostDto?> SetPostHideLikeCountAsync(Guid postId, Guid actorUserId, bool isModerator, bool hideLikeCount, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return null;
        if (post.UserId != actorUserId && !isModerator) throw new UnauthorizedAccessException("post_update_forbidden");
        post.HideLikeCountOverride = hideLikeCount;
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPostDto(postId, actorUserId, cancellationToken);
    }

    public async Task<PagedCommunityCommentsResponse?> GetPostCommentsAsync(Guid postId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var postOwnerId = await dbContext.CommunityPosts
            .Where(x => x.Id == postId && x.Status == CommunityPostStatus.Published)
            .Select(x => (Guid?)x.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (postOwnerId is null) return null;
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.CommunityComments
            .Where(x => x.PostId == postId && x.ParentCommentId == null)
            .OrderByDescending(x => x.CreatedAt == DateTime.MinValue ? x.Post.CreatedAt : x.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.PostId, x.UserId, x.ParentCommentId, x.Body, x.CreatedAt, x.UpdatedAt, PostCreatedAt = x.Post.CreatedAt })
            .ToListAsync(cancellationToken);
        var items = new List<CommunityCommentDto>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(await BuildCommentDtoAsync(row.Id, currentUserId, cancellationToken, includePinnedReply: true, replyCountOverride: await dbContext.CommunityComments.CountAsync(x => x.ParentCommentId == row.Id, cancellationToken), postOwnerId: postOwnerId.Value));
        }
        return new PagedCommunityCommentsResponse(items, page, pageSize, total);
    }

    public async Task<PagedCommunityCommentsResponse?> GetPostCommentsBySlugAsync(string username, string postSlug, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts
            .Where(x => x.Slug == postSlug && x.Status == CommunityPostStatus.Published)
            .Select(x => new { x.Id, x.UserId, x.Slug })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null && PublicSlugGenerator.TryDecodePublicGuidSlug(postSlug, out var decodedPostId))
        {
            post = await dbContext.CommunityPosts
                .Where(x => x.Id == decodedPostId && x.Status == CommunityPostStatus.Published)
                .Select(x => new { x.Id, x.UserId, x.Slug })
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (post is null || !await IsPostOwnerUsernameAsync(post.UserId, username, cancellationToken))
        {
            return null;
        }

        return await GetPostCommentsAsync(post.Id, page, pageSize, currentUserId, cancellationToken);
    }

    public async Task<PagedCommunityCommentsResponse?> GetCommentRepliesAsync(Guid commentId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        if (!await dbContext.CommunityComments.AnyAsync(x => x.Id == commentId && x.Post.Status == CommunityPostStatus.Published, cancellationToken)) return null;
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.CommunityComments
            .Where(x => x.ParentCommentId == commentId)
            .OrderByDescending(x => x.CreatedAt == DateTime.MinValue ? x.Post.CreatedAt : x.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.PostId, x.UserId, x.ParentCommentId, x.Body, x.CreatedAt, x.UpdatedAt, PostCreatedAt = x.Post.CreatedAt })
            .ToListAsync(cancellationToken);
        var items = new List<CommunityCommentDto>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(await BuildCommentDtoAsync(row.Id, currentUserId, cancellationToken));
        }

        return new PagedCommunityCommentsResponse(items, page, pageSize, total);
    }

    public async Task<PagedCommunityCommentsResponse?> GetCommentRepliesBySlugAsync(string username, string postSlug, string commentSlug, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var post = await ResolvePostForSlugAsync(postSlug, cancellationToken);
        if (post is null || !await IsPostOwnerUsernameAsync(post.UserId, username, cancellationToken))
        {
            return null;
        }

        var comment = await dbContext.CommunityComments
            .Where(x => x.Slug == commentSlug && x.PostId == post.Id && x.Post.Status == CommunityPostStatus.Published)
            .Select(x => new CommentSlugResolution(x.Id, x.PostId, x.Post.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (comment is null && PublicSlugGenerator.TryDecodePublicGuidSlug(commentSlug, out var decodedCommentId))
        {
            comment = await dbContext.CommunityComments
                .Where(x => x.Id == decodedCommentId && x.PostId == post.Id && x.Post.Status == CommunityPostStatus.Published)
                .Select(x => new CommentSlugResolution(x.Id, x.PostId, x.Post.UserId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (comment is null)
        {
            return null;
        }

        return await GetCommentRepliesAsync(comment.Id, page, pageSize, currentUserId, cancellationToken);
    }

    public async Task<CommunityCommentDto?> AddCommentAsync(Guid postId, Guid userId, CreateCommunityCommentRequest request, CancellationToken cancellationToken)
    {
        ValidateCommentBody(request.Body);
        var post = await dbContext.CommunityPosts
            .Where(x => x.Id == postId && x.Status == CommunityPostStatus.Published)
            .Select(x => new { x.Id, x.Slug, x.UserId, x.CommentsEnabled })
            .FirstOrDefaultAsync(cancellationToken);
        if (post is null) return null;
        if (!post.CommentsEnabled)
        {
            return null;
        }

        if (!await CanUserCommentOnPostAsync(post.UserId, userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("post_comment_forbidden");
        }

        var comment = new CommunityComment
        {
            PostId = postId,
            UserId = userId,
            Slug = await GenerateUniqueCommentSlugAsync(cancellationToken),
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.CommunityComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishDiscoveryEventAsync(new DiscoveryEventEnvelope(
            "PostCommented",
            "Community",
            "CommunityComment",
            comment.Id.ToString("D"),
            userId,
            null,
            post.UserId,
            null,
            "CommunityPost",
            postId.ToString("D"),
            $"community:comment:{comment.Id:D}",
            comment.CreatedAt,
            System.Text.Json.JsonSerializer.Serialize(new { body = comment.Body })), cancellationToken);
        if (userId != post.UserId)
        {
            await PublishCommunityNotificationAsync(
                async () => await notificationPublisher.PublishPostCommentedAsync(
                    new CommunityInteractionNotification(
                        comment.Id,
                        "CommunityComment",
                        post.UserId,
                        post.Id,
                        post.Slug,
                        comment.Body,
                        comment.CreatedAt == DateTime.MinValue ? DateTime.UtcNow : comment.CreatedAt,
                        await ResolveNotificationActorAsync(userId, cancellationToken),
                        await ResolveNotificationActorAsync(post.UserId, cancellationToken)),
                    cancellationToken));
        }

        return new CommunityCommentDto(comment.Id, comment.Slug, comment.PostId, comment.UserId, comment.ParentCommentId, comment.Body, comment.CreatedAt, comment.UpdatedAt, false, null, 0, [], 0, null, true, false);
    }

    public async Task<CommunityCommentDto?> ReplyCommentAsync(Guid commentId, Guid userId, CreateCommunityReplyRequest request, CancellationToken cancellationToken)
    {
        ValidateCommentBody(request.Body);
        var parent = await dbContext.CommunityComments
            .Where(x => x.Id == commentId)
            .Select(x => new { x.Id, x.PostId, x.UserId, PostSlug = x.Post.Slug, PostOwnerUserId = x.Post.UserId, PostStatus = x.Post.Status, CommentsEnabled = x.Post.CommentsEnabled })
            .FirstOrDefaultAsync(cancellationToken);
        if (parent is null) return null;
        if (parent.PostStatus != CommunityPostStatus.Published) return null;
        if (!parent.CommentsEnabled)
        {
            return null;
        }

        if (!await CanUserCommentOnPostAsync(parent.PostOwnerUserId, userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("post_comment_forbidden");
        }

        var reply = new CommunityComment
        {
            PostId = parent.PostId,
            UserId = userId,
            Slug = await GenerateUniqueCommentSlugAsync(cancellationToken),
            ParentCommentId = parent.Id,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.CommunityComments.Add(reply);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishDiscoveryEventAsync(new DiscoveryEventEnvelope(
            "PostCommented",
            "Community",
            "CommunityComment",
            reply.Id.ToString("D"),
            userId,
            null,
            parent.PostOwnerUserId,
            null,
            "CommunityPost",
            parent.PostId.ToString("D"),
            $"community:comment:{reply.Id:D}",
            reply.CreatedAt,
            System.Text.Json.JsonSerializer.Serialize(new { body = reply.Body })), cancellationToken);
        if (userId != parent.UserId)
        {
            await PublishCommunityNotificationAsync(
                async () => await notificationPublisher.PublishCommentRepliedAsync(
                    new CommunityInteractionNotification(
                        reply.Id,
                        "CommunityComment",
                        parent.UserId,
                        parent.PostId,
                        parent.PostSlug,
                        reply.Body,
                        reply.CreatedAt == DateTime.MinValue ? DateTime.UtcNow : reply.CreatedAt,
                        await ResolveNotificationActorAsync(userId, cancellationToken),
                        await ResolveNotificationActorAsync(parent.PostOwnerUserId, cancellationToken)),
                    cancellationToken));
        }

        return new CommunityCommentDto(reply.Id, reply.Slug, reply.PostId, reply.UserId, reply.ParentCommentId, reply.Body, reply.CreatedAt, reply.UpdatedAt, false, null, 0, [], 0, null, true, false);
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid actorUserId, bool isModerator, CancellationToken cancellationToken)
    {
        var comment = await dbContext.CommunityComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null) return false;
        if (comment.UserId != actorUserId && !isModerator) throw new UnauthorizedAccessException("comment_delete_forbidden");
        var ownerId = await dbContext.CommunityPosts.Where(x => x.Id == comment.PostId).Select(x => x.UserId).FirstOrDefaultAsync(cancellationToken);
        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishDiscoveryEventAsync(new DiscoveryEventEnvelope(
            "PostCommentDeleted",
            "Community",
            "CommunityComment",
            comment.Id.ToString("D"),
            comment.UserId,
            null,
            ownerId == Guid.Empty ? null : ownerId,
            null,
            "CommunityPost",
            comment.PostId.ToString("D"),
            $"community:comment-deleted:{comment.Id:D}",
            DateTime.UtcNow,
            null), cancellationToken);
        return true;
    }

    public Task<ToggleActionResponse?> SetPostLikeAsync(Guid postId, Guid userId, bool liked, CancellationToken cancellationToken)
        => SetPostLikeWithDiscoveryAsync(postId, userId, liked, cancellationToken);

    public Task<ToggleActionResponse?> SetCommentLikeAsync(Guid commentId, Guid userId, bool liked, CancellationToken cancellationToken)
        => SetCommentLikeWithNotificationAsync(commentId, userId, liked, cancellationToken);

    public Task<ToggleActionResponse?> SetSavedPostAsync(Guid postId, Guid userId, bool saved, CancellationToken cancellationToken)
        => TogglePostFlag(postId, userId, saved, dbContext.CommunitySavedPosts, "PostId", (p, u) => new CommunitySavedPost { PostId = p, UserId = u }, cancellationToken);

    public async Task<string?> SetPostInterestAsync(Guid postId, Guid userId, SetCommunityPostInterestRequest request, CancellationToken cancellationToken)
    {
        if (!await IsActivePost(postId, cancellationToken)) return null;
        if (!Enum.TryParse<CommunityPostInterestType>(request.InterestType, true, out var parsed)) throw new ArgumentException("interest_type_invalid");
        var existing = await dbContext.CommunityPostInterests.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
        if (existing is null)
        {
            dbContext.CommunityPostInterests.Add(new CommunityPostInterest { PostId = postId, UserId = userId, InterestType = parsed });
        }
        else
        {
            existing.InterestType = parsed;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return parsed.ToString();
    }

    public async Task<bool> ClearPostInterestAsync(Guid postId, Guid userId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommunityPostInterests.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
        if (existing is null) return false;
        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CommunityReactionSummaryDto>?> SetPostReactionAsync(Guid postId, Guid userId, AddOrUpdateCommunityReactionRequest request, CancellationToken cancellationToken)
    {
        ValidateReaction(request);
        if (!await IsActivePost(postId, cancellationToken)) return null;
        var existing = await dbContext.CommunityPostReactions.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
        if (existing is null)
        {
            dbContext.CommunityPostReactions.Add(new CommunityPostReaction { PostId = postId, UserId = userId, Emoji = request.Emoji.Trim(), EmojiCode = request.EmojiCode.Trim() });
        }
        else { existing.Emoji = request.Emoji.Trim(); existing.EmojiCode = request.EmojiCode.Trim(); existing.UpdatedAt = DateTime.UtcNow; }
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPostReactionSummary(postId, cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityReactionSummaryDto>?> RemovePostReactionAsync(Guid postId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await IsActivePost(postId, cancellationToken)) return null;
        var existing = await dbContext.CommunityPostReactions.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return await BuildPostReactionSummary(postId, cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityReactionSummaryDto>?> SetCommentReactionAsync(Guid commentId, Guid userId, AddOrUpdateCommunityReactionRequest request, CancellationToken cancellationToken)
    {
        ValidateReaction(request);
        if (!await dbContext.CommunityComments.AnyAsync(x => x.Id == commentId, cancellationToken)) return null;
        var existing = await dbContext.CommunityCommentReactions.FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId, cancellationToken);
        if (existing is null)
        {
            dbContext.CommunityCommentReactions.Add(new CommunityCommentReaction { CommentId = commentId, UserId = userId, Emoji = request.Emoji.Trim(), EmojiCode = request.EmojiCode.Trim() });
        }
        else { existing.Emoji = request.Emoji.Trim(); existing.EmojiCode = request.EmojiCode.Trim(); existing.UpdatedAt = DateTime.UtcNow; }
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildCommentReactionSummary(commentId, cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityReactionSummaryDto>?> RemoveCommentReactionAsync(Guid commentId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await dbContext.CommunityComments.AnyAsync(x => x.Id == commentId, cancellationToken)) return null;
        var existing = await dbContext.CommunityCommentReactions.FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return await BuildCommentReactionSummary(commentId, cancellationToken);
    }

    public async Task<bool?> ReportPostAsync(Guid postId, Guid reporterUserId, ReportCommunityPostRequest request, CancellationToken cancellationToken)
    {
        ValidateReport(request.Reason, request.Description);
        var post = await dbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return null;
        if (post.UserId == reporterUserId) throw new ArgumentException("cannot_report_own_post");
        var existing = await dbContext.CommunityPostReports.FirstOrDefaultAsync(x => x.PostId == postId && x.ReporterUserId == reporterUserId, cancellationToken);
        if (existing is not null) return false;
        var reason = Enum.Parse<CommunityReportReason>(request.Reason, true);
        dbContext.CommunityPostReports.Add(new CommunityPostReport { PostId = postId, ReporterUserId = reporterUserId, TargetOwnerUserId = post.UserId, Reason = reason, Description = NormalizeNullable(request.Description) });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool?> ReportCommentAsync(Guid commentId, Guid reporterUserId, ReportCommunityCommentRequest request, CancellationToken cancellationToken)
    {
        ValidateReport(request.Reason, request.Description);
        var comment = await dbContext.CommunityComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null) return null;
        if (comment.UserId == reporterUserId) throw new ArgumentException("cannot_report_own_comment");
        var existing = await dbContext.CommunityCommentReports.FirstOrDefaultAsync(x => x.CommentId == commentId && x.ReporterUserId == reporterUserId, cancellationToken);
        if (existing is not null) return false;
        var reason = Enum.Parse<CommunityReportReason>(request.Reason, true);
        dbContext.CommunityCommentReports.Add(new CommunityCommentReport { CommentId = commentId, ReporterUserId = reporterUserId, TargetOwnerUserId = comment.UserId, Reason = reason, Description = NormalizeNullable(request.Description) });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<ToggleActionResponse?> TogglePostFlag<TEntity>(Guid postId, Guid userId, bool active, DbSet<TEntity> set, string postPropertyName, Func<Guid, Guid, TEntity> factory, CancellationToken cancellationToken) where TEntity : class
    {
        if (!await IsActivePost(postId, cancellationToken)) return null;
        var existing = await set.FirstOrDefaultAsync(x => EF.Property<Guid>(x, postPropertyName) == postId && EF.Property<Guid>(x, "UserId") == userId, cancellationToken);
        if (active && existing is null) set.Add(factory(postId, userId));
        if (!active && existing is not null) set.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await set.CountAsync(x => EF.Property<Guid>(x, postPropertyName) == postId, cancellationToken);
        return new ToggleActionResponse(active && (existing is null || active), count);
    }

    private async Task<ToggleActionResponse?> SetPostLikeWithDiscoveryAsync(Guid postId, Guid userId, bool liked, CancellationToken cancellationToken)
    {
        if (!await IsActivePost(postId, cancellationToken)) return null;
        var post = await dbContext.CommunityPosts.Where(x => x.Id == postId).Select(x => new { x.Id, x.Slug, x.UserId }).FirstOrDefaultAsync(cancellationToken);
        if (post is null) return null;
        var existing = await dbContext.CommunityPostLikes.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
        var changed = liked ? existing is null : existing is not null;
        Guid? sourceLikeId = existing?.Id;
        if (liked && existing is null)
        {
            var postLike = new CommunityPostLike { PostId = postId, UserId = userId };
            sourceLikeId = postLike.Id;
            dbContext.CommunityPostLikes.Add(postLike);
        }

        if (!liked && existing is not null) dbContext.CommunityPostLikes.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.CommunityPostLikes.CountAsync(x => x.PostId == postId, cancellationToken);

        if (changed && sourceLikeId.HasValue)
        {
            await PublishDiscoveryEventAsync(new DiscoveryEventEnvelope(
                liked ? "PostLiked" : "PostUnliked",
                "Community",
                "CommunityPostLike",
                sourceLikeId.Value.ToString("D"),
                userId,
                null,
                post.UserId,
                null,
                "CommunityPost",
                postId.ToString("D"),
                $"community:post-like:{sourceLikeId.Value:D}:{liked}",
                DateTime.UtcNow,
                null), cancellationToken);
        }

        if (changed && liked && userId != post.UserId && sourceLikeId.HasValue)
        {
            await PublishCommunityNotificationAsync(
                async () => await notificationPublisher.PublishPostLikedAsync(
                    new CommunityInteractionNotification(
                        sourceLikeId.Value,
                        "CommunityPostLike",
                        post.UserId,
                        post.Id,
                        post.Slug,
                        null,
                        DateTime.UtcNow,
                        await ResolveNotificationActorAsync(userId, cancellationToken),
                        await ResolveNotificationActorAsync(post.UserId, cancellationToken)),
                    cancellationToken));
        }

        return new ToggleActionResponse(liked, count);
    }

    private async Task PublishDiscoveryEventAsync(DiscoveryEventEnvelope discoveryEvent, CancellationToken cancellationToken)
    {
        try
        {
            await discoveryEventPublisher.PublishAsync(discoveryEvent, cancellationToken);
        }
        catch
        {
            // Discovery events are best-effort until an outbox/message-bus publisher is wired.
        }
    }

    private async Task PublishCommunityNotificationAsync(Func<Task> publishAsync)
    {
        try
        {
            await publishAsync();
        }
        catch
        {
            // Community notifications are best-effort and must not roll back the user action.
        }
    }

    private async Task<CommunityNotificationActor> ResolveNotificationActorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var summaries = await authorProfileProvider.GetAuthorSummariesAsync([userId], null, cancellationToken);
        return summaries.TryGetValue(userId, out var summary)
            ? new CommunityNotificationActor(summary.UserId, summary.Username, summary.DisplayName, summary.AvatarUrl)
            : new CommunityNotificationActor(userId, null, null, null);
    }

    private async Task<ToggleActionResponse?> SetCommentLikeWithNotificationAsync(Guid commentId, Guid userId, bool liked, CancellationToken cancellationToken)
    {
        var comment = await dbContext.CommunityComments
            .Where(x => x.Id == commentId)
            .Select(x => new { x.Id, x.UserId, x.Body, x.PostId, PostSlug = x.Post.Slug, PostOwnerUserId = x.Post.UserId, PostStatus = x.Post.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (comment is null || comment.PostStatus != CommunityPostStatus.Published) return null;

        var existing = await dbContext.CommunityCommentLikes.FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId, cancellationToken);
        var changed = liked ? existing is null : existing is not null;
        Guid? sourceLikeId = existing?.Id;
        if (liked && existing is null)
        {
            var commentLike = new CommunityCommentLike { CommentId = commentId, UserId = userId };
            sourceLikeId = commentLike.Id;
            dbContext.CommunityCommentLikes.Add(commentLike);
        }

        if (!liked && existing is not null) dbContext.CommunityCommentLikes.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.CommunityCommentLikes.CountAsync(x => x.CommentId == commentId, cancellationToken);

        if (changed && liked && userId != comment.UserId && sourceLikeId.HasValue)
        {
            await PublishCommunityNotificationAsync(
                async () => await notificationPublisher.PublishCommentLikedAsync(
                    new CommunityInteractionNotification(
                        sourceLikeId.Value,
                        "CommunityCommentLike",
                        comment.UserId,
                        comment.PostId,
                        comment.PostSlug,
                        comment.Body,
                        DateTime.UtcNow,
                        await ResolveNotificationActorAsync(userId, cancellationToken),
                        await ResolveNotificationActorAsync(comment.PostOwnerUserId, cancellationToken)),
                    cancellationToken));
        }

        return new ToggleActionResponse(liked, count);
    }

    private async Task<ToggleActionResponse?> ToggleCommentFlag<TEntity>(Guid commentId, Guid userId, bool active, DbSet<TEntity> set, string commentPropertyName, Func<Guid, Guid, TEntity> factory, CancellationToken cancellationToken) where TEntity : class
    {
        if (!await dbContext.CommunityComments.AnyAsync(x => x.Id == commentId, cancellationToken)) return null;
        var existing = await set.FirstOrDefaultAsync(x => EF.Property<Guid>(x, commentPropertyName) == commentId && EF.Property<Guid>(x, "UserId") == userId, cancellationToken);
        if (active && existing is null) set.Add(factory(commentId, userId));
        if (!active && existing is not null) set.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await set.CountAsync(x => EF.Property<Guid>(x, commentPropertyName) == commentId, cancellationToken);
        return new ToggleActionResponse(active && (existing is null || active), count);
    }

    private async Task<CommunityCommentDto> BuildCommentDtoAsync(
        Guid commentId,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        bool includePinnedReply = false,
        int? replyCountOverride = null,
        Guid? postOwnerId = null)
    {
        var row = await dbContext.CommunityComments
            .Where(x => x.Id == commentId)
            .Select(x => new { x.Id, x.Slug, x.PostId, x.UserId, x.ParentCommentId, x.Body, x.CreatedAt, x.UpdatedAt, PostCreatedAt = x.Post.CreatedAt })
            .FirstAsync(cancellationToken);
        var createdAt = row.CreatedAt == DateTime.MinValue ? row.PostCreatedAt : row.CreatedAt;

        var likes = await dbContext.CommunityCommentLikes.CountAsync(x => x.CommentId == row.Id, cancellationToken);
        var isLiked = currentUserId.HasValue && await dbContext.CommunityCommentLikes.AnyAsync(x => x.CommentId == row.Id && x.UserId == currentUserId.Value, cancellationToken);
        var currentReaction = currentUserId.HasValue ? await dbContext.CommunityCommentReactions.Where(x => x.CommentId == row.Id && x.UserId == currentUserId.Value).Select(x => x.EmojiCode).FirstOrDefaultAsync(cancellationToken) : null;
        var summary = await BuildCommentReactionSummary(row.Id, cancellationToken);
        CommunityCommentDto? pinnedReply = null;

        if (includePinnedReply && postOwnerId.HasValue)
        {
            var pinnedReplyRow = await dbContext.CommunityComments
                .Where(x => x.ParentCommentId == row.Id && x.UserId == postOwnerId.Value)
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (pinnedReplyRow != Guid.Empty)
            {
                pinnedReply = await BuildCommentDtoAsync(pinnedReplyRow, currentUserId, cancellationToken);
            }
        }

        var replyCount = replyCountOverride ?? await dbContext.CommunityComments.CountAsync(x => x.ParentCommentId == row.Id, cancellationToken);
        return new CommunityCommentDto(
            row.Id,
            row.Slug,
            row.PostId,
            row.UserId,
            row.ParentCommentId,
            row.Body,
            createdAt,
            row.UpdatedAt,
            isLiked,
            currentReaction,
            likes,
            summary,
            replyCount,
            pinnedReply,
            currentUserId == row.UserId,
            currentUserId != row.UserId);
    }

    private async Task<CommunityPostDto?> BuildPostDto(
        Guid postId,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, CommunityAuthorSummary>? authorSummaries = null,
        bool authorVisibilityAlreadyChecked = false)
    {
        var row = await dbContext.CommunityPosts.Where(x => x.Id == postId && x.Status == CommunityPostStatus.Published)
            .Select(x => new { x.Id, x.Slug, x.UserId, x.Caption, x.City, x.District, x.Status, x.CommentsEnabled, x.HideLikeCountOverride, x.CreatedAt, x.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        if (!authorVisibilityAlreadyChecked && !await visibilityService.CanViewAuthorPostsAsync(row.UserId, currentUserId, cancellationToken))
        {
            return null;
        }

        var commentsCount = await dbContext.CommunityComments.CountAsync(x => x.PostId == postId, cancellationToken);
        var likesCount = await dbContext.CommunityPostLikes.CountAsync(x => x.PostId == postId, cancellationToken);
        var savesCount = await dbContext.CommunitySavedPosts.CountAsync(x => x.PostId == postId, cancellationToken);
        var isLiked = currentUserId.HasValue && await dbContext.CommunityPostLikes.AnyAsync(x => x.PostId == postId && x.UserId == currentUserId.Value, cancellationToken);
        var isSaved = currentUserId.HasValue && await dbContext.CommunitySavedPosts.AnyAsync(x => x.PostId == postId && x.UserId == currentUserId.Value, cancellationToken);
        var reaction = currentUserId.HasValue ? await dbContext.CommunityPostReactions.Where(x => x.PostId == postId && x.UserId == currentUserId.Value).Select(x => x.EmojiCode).FirstOrDefaultAsync(cancellationToken) : null;
        var interest = currentUserId.HasValue ? await dbContext.CommunityPostInterests.Where(x => x.PostId == postId && x.UserId == currentUserId.Value).Select(x => x.InterestType.ToString()).FirstOrDefaultAsync(cancellationToken) : null;
        var media = await dbContext.CommunityPostMedia.Where(x => x.PostId == postId).OrderBy(x => x.Order).Select(x => new CommunityPostMediaDto(x.Id, x.PublicUrl, x.ContentType, x.SizeBytes, x.Width, x.Height, x.Order, x.Status.ToString())).ToListAsync(cancellationToken);

        var canEdit = currentUserId.HasValue && currentUserId.Value == row.UserId;
        var summaryMap = authorSummaries ?? await authorProfileProvider.GetAuthorSummariesAsync([row.UserId], currentUserId, cancellationToken);
        summaryMap.TryGetValue(row.UserId, out var summary);
        var author = summary is null
            ? new CommunityAuthorDto(row.UserId, null, null, null, false, false)
            : new CommunityAuthorDto(row.UserId, summary.Username, summary.DisplayName, summary.AvatarUrl, summary.IsVerified, summary.HideLikeCounts);
        return new CommunityPostDto(row.Id, row.Slug, row.UserId, row.Caption, row.City, row.District, row.Status.ToString(), row.CreatedAt, row.UpdatedAt, row.CommentsEnabled, row.HideLikeCountOverride, commentsCount, likesCount, savesCount, isLiked, isSaved, reaction, interest, canEdit, canEdit, currentUserId.HasValue && currentUserId.Value != row.UserId, author, media);
    }

    private Task<List<CommunityReactionSummaryDto>> BuildPostReactionSummary(Guid postId, CancellationToken cancellationToken)
        => dbContext.CommunityPostReactions.Where(x => x.PostId == postId).GroupBy(x => new { x.EmojiCode, x.Emoji }).Select(g => new CommunityReactionSummaryDto(g.Key.EmojiCode, g.Key.Emoji, g.Count())).ToListAsync(cancellationToken);

    private Task<List<CommunityReactionSummaryDto>> BuildCommentReactionSummary(Guid commentId, CancellationToken cancellationToken)
        => dbContext.CommunityCommentReactions.Where(x => x.CommentId == commentId).GroupBy(x => new { x.EmojiCode, x.Emoji }).Select(g => new CommunityReactionSummaryDto(g.Key.EmojiCode, g.Key.Emoji, g.Count())).ToListAsync(cancellationToken);

    private async Task<string> GenerateUniquePostSlugAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var slug = PublicSlugGenerator.CreateNumericSlug();
            var exists = await dbContext.CommunityPosts.AnyAsync(x => x.Slug == slug, cancellationToken);
            if (!exists)
            {
                return slug;
            }
        }
    }

    private async Task<string> GenerateUniqueCommentSlugAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var slug = PublicSlugGenerator.CreateNumericSlug();
            var exists = await dbContext.CommunityComments.AnyAsync(x => x.Slug == slug, cancellationToken);
            if (!exists)
            {
                return slug;
            }
        }
    }

    private async Task<bool> IsPostOwnerUsernameAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var summaries = await authorProfileProvider.GetAuthorSummariesAsync([userId], null, cancellationToken);
        return summaries.TryGetValue(userId, out var summary)
            && string.Equals(summary.Username, username, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<PostSlugResolution?> ResolvePostForSlugAsync(string postSlug, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts
            .Where(x => x.Slug == postSlug && x.Status == CommunityPostStatus.Published)
            .Select(x => new PostSlugResolution(x.Id, x.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (post is not null)
        {
            return post;
        }

        if (!PublicSlugGenerator.TryDecodePublicGuidSlug(postSlug, out var decodedPostId))
        {
            return null;
        }

        return await dbContext.CommunityPosts
            .Where(x => x.Id == decodedPostId && x.Status == CommunityPostStatus.Published)
            .Select(x => new PostSlugResolution(x.Id, x.UserId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<bool> IsActivePost(Guid postId, CancellationToken cancellationToken)
        => dbContext.CommunityPosts.AnyAsync(x => x.Id == postId && x.Status == CommunityPostStatus.Published, cancellationToken);

    private async Task<List<VisibleFeedCandidate>> LoadVisibleFeedCandidatesAsync(
        IQueryable<CommunityPost> baseQuery,
        int page,
        int pageSize,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var targetVisibleCount = (page * pageSize) + 1;
        var scanSize = Math.Clamp(pageSize * 5, 20, 100);
        var visible = new List<VisibleFeedCandidate>(targetVisibleCount);
        DateTime? lastCreatedAt = null;
        Guid? lastPostId = null;

        while (visible.Count < targetVisibleCount)
        {
            var chunkQuery = baseQuery;
            if (lastCreatedAt.HasValue && lastPostId.HasValue)
            {
                var createdAt = lastCreatedAt.Value;
                var postId = lastPostId.Value;
                chunkQuery = chunkQuery.Where(post =>
                    post.CreatedAt < createdAt ||
                    (post.CreatedAt == createdAt && post.Id.CompareTo(postId) < 0));
            }

            var chunk = await chunkQuery
                .OrderByDescending(post => post.CreatedAt)
                .ThenByDescending(post => post.Id)
                .Select(post => new VisibleFeedCandidate(post.Id, post.UserId, post.CreatedAt))
                .Take(scanSize)
                .ToListAsync(cancellationToken);

            if (chunk.Count == 0)
            {
                break;
            }

            var authorSummaries = await authorProfileProvider.GetAuthorSummariesAsync(chunk.Select(x => x.UserId).Distinct().ToArray(), currentUserId, cancellationToken);
            foreach (var candidate in chunk)
            {
                if (!authorSummaries.TryGetValue(candidate.UserId, out var summary) || !summary.CanViewPosts)
                {
                    continue;
                }

                visible.Add(candidate);
                if (visible.Count >= targetVisibleCount)
                {
                    break;
                }
            }

            if (chunk.Count < scanSize || visible.Count >= targetVisibleCount)
            {
                break;
            }

            var last = chunk[^1];
            lastCreatedAt = last.CreatedAt;
            lastPostId = last.Id;
        }

        return visible;
    }

    private async Task<IReadOnlyList<CommunityFeedItemDto>> BuildFeedItemsAsync(
        IReadOnlyList<Guid> postIds,
        Guid? currentUserId,
        IReadOnlyDictionary<Guid, CommunityAuthorSummary> authorSummaries,
        CancellationToken cancellationToken)
    {
        if (postIds.Count == 0)
        {
            return [];
        }

        var posts = await dbContext.CommunityPosts
            .AsNoTracking()
            .Where(x => postIds.Contains(x.Id) && x.Status == CommunityPostStatus.Published)
            .Select(x => new { x.Id, x.Slug, x.UserId, x.Caption, x.City, x.District, x.Status, x.CommentsEnabled, x.HideLikeCountOverride, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(cancellationToken);

        var postMap = posts.ToDictionary(x => x.Id);
        var commentsCounts = await dbContext.CommunityComments
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .GroupBy(x => x.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);
        var likesCounts = await dbContext.CommunityPostLikes
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .GroupBy(x => x.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);
        var savesCounts = await dbContext.CommunitySavedPosts
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .GroupBy(x => x.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);

        HashSet<Guid> likedPostIds = [];
        HashSet<Guid> savedPostIds = [];
        Dictionary<Guid, string> reactionByPostId = [];
        Dictionary<Guid, string> interestByPostId = [];
        if (currentUserId.HasValue)
        {
            var viewerId = currentUserId.Value;
            likedPostIds = (await dbContext.CommunityPostLikes
                .AsNoTracking()
                .Where(x => postIds.Contains(x.PostId) && x.UserId == viewerId)
                .Select(x => x.PostId)
                .ToArrayAsync(cancellationToken)).ToHashSet();

            savedPostIds = (await dbContext.CommunitySavedPosts
                .AsNoTracking()
                .Where(x => postIds.Contains(x.PostId) && x.UserId == viewerId)
                .Select(x => x.PostId)
                .ToArrayAsync(cancellationToken)).ToHashSet();

            reactionByPostId = await dbContext.CommunityPostReactions
                .AsNoTracking()
                .Where(x => postIds.Contains(x.PostId) && x.UserId == viewerId)
                .Select(x => new { x.PostId, x.EmojiCode })
                .ToDictionaryAsync(x => x.PostId, x => x.EmojiCode, cancellationToken);

            interestByPostId = await dbContext.CommunityPostInterests
                .AsNoTracking()
                .Where(x => postIds.Contains(x.PostId) && x.UserId == viewerId)
                .Select(x => new { x.PostId, Interest = x.InterestType.ToString() })
                .ToDictionaryAsync(x => x.PostId, x => x.Interest, cancellationToken);
        }

        var mediaRows = await dbContext.CommunityPostMedia
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .OrderBy(x => x.PostId)
            .ThenBy(x => x.Order)
            .Select(x => new
            {
                x.PostId,
                Media = new CommunityPostMediaDto(x.Id, x.PublicUrl, x.ContentType, x.SizeBytes, x.Width, x.Height, x.Order, x.Status.ToString())
            })
            .ToListAsync(cancellationToken);
        var mediaByPostId = mediaRows
            .GroupBy(x => x.PostId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CommunityPostMediaDto>)g.Select(item => item.Media).ToList());
        var reactionRows = await dbContext.CommunityPostReactions
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .GroupBy(x => new { x.PostId, x.EmojiCode, x.Emoji })
            .Select(g => new { g.Key.PostId, g.Key.EmojiCode, g.Key.Emoji, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var reactionsByPostId = reactionRows
            .GroupBy(x => x.PostId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CommunityReactionSummaryDto>)g.Select(item => new CommunityReactionSummaryDto(item.EmojiCode, item.Emoji, item.Count)).ToList());

        var orderedIds = postIds
            .Where(postMap.ContainsKey)
            .ToArray();
        var items = new List<CommunityFeedItemDto>(orderedIds.Length);
        foreach (var postId in orderedIds)
        {
            var post = postMap[postId];
            authorSummaries.TryGetValue(post.UserId, out var summary);
            var author = summary is null
                ? new CommunityAuthorDto(post.UserId, null, null, null, false, false)
                : new CommunityAuthorDto(post.UserId, summary.Username, summary.DisplayName, summary.AvatarUrl, summary.IsVerified, summary.HideLikeCounts);

            mediaByPostId.TryGetValue(postId, out var media);
            reactionsByPostId.TryGetValue(postId, out var reactionSummary);

            items.Add(new CommunityFeedItemDto(
                new CommunityPostDto(
                    post.Id,
                    post.Slug,
                    post.UserId,
                    post.Caption,
                    post.City,
                    post.District,
                    post.Status.ToString(),
                    post.CreatedAt,
                    post.UpdatedAt,
                    post.CommentsEnabled,
                    post.HideLikeCountOverride,
                    commentsCounts.TryGetValue(postId, out var commentsCount) ? commentsCount : 0,
                    likesCounts.TryGetValue(postId, out var likesCount) ? likesCount : 0,
                    savesCounts.TryGetValue(postId, out var savesCount) ? savesCount : 0,
                    likedPostIds.Contains(postId),
                    savedPostIds.Contains(postId),
                    reactionByPostId.TryGetValue(postId, out var reaction) ? reaction : null,
                    interestByPostId.TryGetValue(postId, out var interest) ? interest : null,
                    currentUserId.HasValue && currentUserId.Value == post.UserId,
                    currentUserId.HasValue && currentUserId.Value == post.UserId,
                    currentUserId.HasValue && currentUserId.Value != post.UserId,
                    author,
                    media ?? []),
                reactionSummary ?? []));
        }

        return items;
    }

    private static (int page, int pageSize) NormalizePage(int page, int pageSize) => (page <= 0 ? 1 : page, pageSize is <= 0 or > 100 ? 20 : pageSize);
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<bool> CanUserCommentOnPostAsync(Guid authorUserId, Guid commenterUserId, CancellationToken cancellationToken)
    {
        if (authorUserId == commenterUserId)
        {
            return true;
        }

        var summaries = await authorProfileProvider.GetAuthorSummariesAsync([authorUserId], commenterUserId, cancellationToken);
        if (!summaries.TryGetValue(authorUserId, out var summary))
        {
            return false;
        }

        return summary.CommentAudience switch
        {
            null or "" => summary.IsFollowedByCurrentUser,
            "MutualFollowers" => summary.IsFollowedByCurrentUser && summary.IsFollowingCurrentUser,
            "Closed" => false,
            _ => summary.IsFollowedByCurrentUser,
        };
    }

    private static void ValidateCaption(string? caption)
    {
        if (!string.IsNullOrEmpty(caption) && caption.Length > 2200) throw new ArgumentException("caption_max_length");
    }

    private static void ValidateCommentBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("comment_body_required");
        if (body.Trim().Length > 1000) throw new ArgumentException("comment_body_max_length");
    }

    private static void ValidateReaction(AddOrUpdateCommunityReactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Emoji) || request.Emoji.Trim().Length > 32) throw new ArgumentException("reaction_emoji_invalid");
        if (string.IsNullOrWhiteSpace(request.EmojiCode) || request.EmojiCode.Trim().Length > 64) throw new ArgumentException("reaction_emoji_code_invalid");
    }

    private static void ValidateReport(string reason, string? description)
    {
        if (!Enum.TryParse<CommunityReportReason>(reason, true, out _)) throw new ArgumentException("report_reason_invalid");
        if (!string.IsNullOrEmpty(description) && description.Length > 1000) throw new ArgumentException("report_description_max_length");
    }

    private sealed record PostSlugResolution(Guid Id, Guid UserId);
    private sealed record CommentSlugResolution(Guid Id, Guid PostId, Guid UserId);
    private sealed record VisibleFeedCandidate(Guid Id, Guid UserId, DateTime CreatedAt);
}
