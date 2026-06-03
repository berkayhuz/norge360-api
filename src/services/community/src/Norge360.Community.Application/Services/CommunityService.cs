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

namespace Norge360.Community.Application.Services;

public sealed class CommunityService(
    ICommunityDbContext dbContext,
    ICommunityAuthorProfileProvider authorProfileProvider,
    ICommunityVisibilityService visibilityService,
    IDiscoveryEventPublisher discoveryEventPublisher) : ICommunityService
{
    public async Task<CommunityPostDto> CreatePostAsync(Guid userId, CreateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        ValidateCaption(request.Caption);
        var post = new CommunityPost
        {
            UserId = userId,
            Caption = NormalizeNullable(request.Caption),
            City = NormalizeNullable(request.City),
            District = NormalizeNullable(request.District),
            Status = CommunityPostStatus.Published
        };
        dbContext.CommunityPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPostDto(post.Id, userId, cancellationToken) ?? throw new InvalidOperationException("post_create_failed");
    }

    public Task<CommunityPostDto?> GetPostAsync(Guid postId, Guid? currentUserId, CancellationToken cancellationToken)
        => BuildPostDto(postId, currentUserId, cancellationToken);

    public async Task<PagedCommunityFeedResponse> GetFeedAsync(int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var blockedIds = currentUserId.HasValue
            ? await dbContext.CommunityPostInterests.Where(x => x.UserId == currentUserId.Value && x.InterestType == CommunityPostInterestType.NotInterested).Select(x => x.PostId).ToListAsync(cancellationToken)
            : [];

        var baseQuery = dbContext.CommunityPosts.Where(x => x.Status == CommunityPostStatus.Published && !blockedIds.Contains(x.Id));
        var visibleAuthorIds = (await visibilityService.FilterVisibleAuthorsAsync(await baseQuery.Select(x => x.UserId).Distinct().ToListAsync(cancellationToken), currentUserId, cancellationToken)).ToList();
        var query = baseQuery.Where(x => visibleAuthorIds.Contains(x.UserId));
        var total = await query.CountAsync(cancellationToken);
        var ids = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(cancellationToken);
        var postAuthorIds = await dbContext.CommunityPosts.Where(x => ids.Contains(x.Id)).Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        var authorSummaries = await authorProfileProvider.GetAuthorSummariesAsync(postAuthorIds, currentUserId, cancellationToken);
        var items = new List<CommunityFeedItemDto>(ids.Count);
        foreach (var id in ids)
        {
            var post = await BuildPostDto(id, currentUserId, cancellationToken, authorSummaries, authorVisibilityAlreadyChecked: true);
            if (post is null) continue;
            var summary = await BuildPostReactionSummary(id, cancellationToken);
            items.Add(new CommunityFeedItemDto(post, summary));
        }

        return new PagedCommunityFeedResponse(items, page, pageSize, total);
    }

    public async Task<PagedCommunityFeedResponse> GetUserPostsAsync(Guid userId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        if (!await visibilityService.CanViewAuthorPostsAsync(userId, currentUserId, cancellationToken))
        {
            return new PagedCommunityFeedResponse([], page, pageSize, 0);
        }

        var query = dbContext.CommunityPosts.Where(x => x.UserId == userId && x.Status == CommunityPostStatus.Published);
        var total = await query.CountAsync(cancellationToken);
        var ids = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(cancellationToken);
        var authorSummaries = await authorProfileProvider.GetAuthorSummariesAsync([userId], currentUserId, cancellationToken);
        var items = new List<CommunityFeedItemDto>(ids.Count);
        foreach (var id in ids)
        {
            var post = await BuildPostDto(id, currentUserId, cancellationToken, authorSummaries, authorVisibilityAlreadyChecked: true);
            if (post is null) continue;
            items.Add(new CommunityFeedItemDto(post, await BuildPostReactionSummary(id, cancellationToken)));
        }
        return new PagedCommunityFeedResponse(items, page, pageSize, total);
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

    public async Task<PagedCommunityCommentsResponse?> GetPostCommentsAsync(Guid postId, int page, int pageSize, Guid? currentUserId, CancellationToken cancellationToken)
    {
        if (!await dbContext.CommunityPosts.AnyAsync(x => x.Id == postId && x.Status == CommunityPostStatus.Published, cancellationToken)) return null;
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.CommunityComments.Where(x => x.PostId == postId).OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, x.PostId, x.UserId, x.ParentCommentId, x.Body, x.CreatedAt, x.UpdatedAt }).ToListAsync(cancellationToken);
        var items = new List<CommunityCommentDto>(rows.Count);
        foreach (var row in rows)
        {
            var likes = await dbContext.CommunityCommentLikes.CountAsync(x => x.CommentId == row.Id, cancellationToken);
            var isLiked = currentUserId.HasValue && await dbContext.CommunityCommentLikes.AnyAsync(x => x.CommentId == row.Id && x.UserId == currentUserId.Value, cancellationToken);
            var currentReaction = currentUserId.HasValue ? await dbContext.CommunityCommentReactions.Where(x => x.CommentId == row.Id && x.UserId == currentUserId.Value).Select(x => x.EmojiCode).FirstOrDefaultAsync(cancellationToken) : null;
            var summary = await BuildCommentReactionSummary(row.Id, cancellationToken);
            items.Add(new CommunityCommentDto(row.Id, row.PostId, row.UserId, row.ParentCommentId, row.Body, row.CreatedAt, row.UpdatedAt, isLiked, currentReaction, likes, summary, currentUserId == row.UserId, currentUserId != row.UserId));
        }
        return new PagedCommunityCommentsResponse(items, page, pageSize, total);
    }

    public async Task<CommunityCommentDto?> AddCommentAsync(Guid postId, Guid userId, CreateCommunityCommentRequest request, CancellationToken cancellationToken)
    {
        ValidateCommentBody(request.Body);
        if (!await dbContext.CommunityPosts.AnyAsync(x => x.Id == postId && x.Status == CommunityPostStatus.Published, cancellationToken)) return null;
        var comment = new CommunityComment { PostId = postId, UserId = userId, Body = request.Body.Trim() };
        dbContext.CommunityComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        var ownerId = await dbContext.CommunityPosts.Where(x => x.Id == postId).Select(x => x.UserId).FirstOrDefaultAsync(cancellationToken);
        await PublishDiscoveryEventAsync(new DiscoveryEventEnvelope(
            "PostCommented",
            "Community",
            "CommunityComment",
            comment.Id.ToString("D"),
            userId,
            null,
            ownerId == Guid.Empty ? null : ownerId,
            null,
            "CommunityPost",
            postId.ToString("D"),
            $"community:comment:{comment.Id:D}",
            comment.CreatedAt,
            System.Text.Json.JsonSerializer.Serialize(new { body = comment.Body })), cancellationToken);
        return new CommunityCommentDto(comment.Id, comment.PostId, comment.UserId, comment.ParentCommentId, comment.Body, comment.CreatedAt, comment.UpdatedAt, false, null, 0, [], true, false);
    }

    public async Task<CommunityCommentDto?> ReplyCommentAsync(Guid commentId, Guid userId, CreateCommunityReplyRequest request, CancellationToken cancellationToken)
    {
        ValidateCommentBody(request.Body);
        var parent = await dbContext.CommunityComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (parent is null) return null;
        if (!await IsActivePost(parent.PostId, cancellationToken)) return null;
        var reply = new CommunityComment { PostId = parent.PostId, UserId = userId, ParentCommentId = parent.Id, Body = request.Body.Trim() };
        dbContext.CommunityComments.Add(reply);
        await dbContext.SaveChangesAsync(cancellationToken);
        var ownerId = await dbContext.CommunityPosts.Where(x => x.Id == parent.PostId).Select(x => x.UserId).FirstOrDefaultAsync(cancellationToken);
        await PublishDiscoveryEventAsync(new DiscoveryEventEnvelope(
            "PostCommented",
            "Community",
            "CommunityComment",
            reply.Id.ToString("D"),
            userId,
            null,
            ownerId == Guid.Empty ? null : ownerId,
            null,
            "CommunityPost",
            parent.PostId.ToString("D"),
            $"community:comment:{reply.Id:D}",
            reply.CreatedAt,
            System.Text.Json.JsonSerializer.Serialize(new { body = reply.Body })), cancellationToken);
        return new CommunityCommentDto(reply.Id, reply.PostId, reply.UserId, reply.ParentCommentId, reply.Body, reply.CreatedAt, reply.UpdatedAt, false, null, 0, [], true, false);
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
        => ToggleCommentFlag(commentId, userId, liked, dbContext.CommunityCommentLikes, "CommentId", (c, u) => new CommunityCommentLike { CommentId = c, UserId = u }, cancellationToken);

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
        var post = await dbContext.CommunityPosts.Where(x => x.Id == postId).Select(x => new { x.Id, x.UserId }).FirstOrDefaultAsync(cancellationToken);
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

    private async Task<CommunityPostDto?> BuildPostDto(
        Guid postId,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, CommunityAuthorSummary>? authorSummaries = null,
        bool authorVisibilityAlreadyChecked = false)
    {
        var row = await dbContext.CommunityPosts.Where(x => x.Id == postId && x.Status == CommunityPostStatus.Published)
            .Select(x => new { x.Id, x.UserId, x.Caption, x.City, x.District, x.Status, x.CreatedAt, x.UpdatedAt })
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
            ? new CommunityAuthorDto(row.UserId, null, null, null, false)
            : new CommunityAuthorDto(row.UserId, summary.Username, summary.DisplayName, summary.AvatarUrl, summary.IsVerified);
        return new CommunityPostDto(row.Id, row.UserId, row.Caption, row.City, row.District, row.Status.ToString(), row.CreatedAt, row.UpdatedAt, commentsCount, likesCount, savesCount, isLiked, isSaved, reaction, interest, canEdit, canEdit, currentUserId.HasValue && currentUserId.Value != row.UserId, author, media);
    }

    private Task<List<CommunityReactionSummaryDto>> BuildPostReactionSummary(Guid postId, CancellationToken cancellationToken)
        => dbContext.CommunityPostReactions.Where(x => x.PostId == postId).GroupBy(x => new { x.EmojiCode, x.Emoji }).Select(g => new CommunityReactionSummaryDto(g.Key.EmojiCode, g.Key.Emoji, g.Count())).ToListAsync(cancellationToken);

    private Task<List<CommunityReactionSummaryDto>> BuildCommentReactionSummary(Guid commentId, CancellationToken cancellationToken)
        => dbContext.CommunityCommentReactions.Where(x => x.CommentId == commentId).GroupBy(x => new { x.EmojiCode, x.Emoji }).Select(g => new CommunityReactionSummaryDto(g.Key.EmojiCode, g.Key.Emoji, g.Count())).ToListAsync(cancellationToken);

    private Task<bool> IsActivePost(Guid postId, CancellationToken cancellationToken)
        => dbContext.CommunityPosts.AnyAsync(x => x.Id == postId && x.Status == CommunityPostStatus.Published, cancellationToken);

    private static (int page, int pageSize) NormalizePage(int page, int pageSize) => (page <= 0 ? 1 : page, pageSize is <= 0 or > 100 ? 20 : pageSize);
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
}
