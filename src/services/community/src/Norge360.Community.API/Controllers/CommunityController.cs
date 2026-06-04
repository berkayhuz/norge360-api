// <copyright file="CommunityController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Norge360.Community.API.Models;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;
using Norge360.Community.Contracts.Requests;
using Norge360.Community.Contracts.Responses;
using Norge360.Community.Domain.Entities;
using Norge360.Community.Domain.Enums;
using Norge360.CurrentUser;

namespace Norge360.Community.API.Controllers;

[ApiController]
[Route("api/community")]
public sealed class CommunityController(
    ICommunityService communityService,
    ICommunityMediaService communityMediaService,
    ICommunityDbContext communityDbContext,
    ICurrentUserService currentUserService,
    IDistributedCache distributedCache) : ControllerBase
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan FeedCacheTtl = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PostCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UserPostsCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CommentsCacheTtl = TimeSpan.FromSeconds(20);

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult GetHealth() => Ok(new { service = "community", status = "ok" });

    [HttpPost("posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromForm] CommunityUpsertPostFormRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var mediaFiles = request.MediaFiles ?? [];
            if (mediaFiles.Count > 8) return BadRequest(new { errorCode = "community_media_too_many_files" });
            if (string.IsNullOrWhiteSpace(request.Caption) && mediaFiles.Count == 0) return BadRequest(new { errorCode = "community_post_empty" });

            var post = await communityService.CreatePostAsync(userId.Value, new CreateCommunityPostRequest(request.Caption, request.City, request.District), cancellationToken);
            if (mediaFiles.Count == 0) return Ok(post);

            IReadOnlyList<CommunityUploadedMedia> uploaded = [];
            try
            {
                var payloads = await ToPayloadsAsync(mediaFiles, cancellationToken);
                uploaded = await communityMediaService.UploadPostMediaAsync(post.Id, userId.Value, payloads, cancellationToken);
                foreach (var item in uploaded.Select((x, i) => new { Media = x, Order = i }))
                {
                    communityDbContext.CommunityPostMedia.Add(new CommunityPostMedia
                    {
                        PostId = post.Id,
                        StorageKey = item.Media.StorageKey,
                        PublicUrl = item.Media.PublicUrl,
                        ContentType = item.Media.ContentType,
                        SizeBytes = item.Media.SizeBytes,
                        Width = item.Media.Width,
                        Height = item.Media.Height,
                        Order = (short)item.Order,
                        Status = CommunityMediaStatus.Ready
                    });
                }
                await communityDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await CleanupUploadedMediaAsync(uploaded, cancellationToken);
                await TryDeleteCreatedPostAsync(post.Id, userId.Value, cancellationToken);
                throw;
            }

            var enriched = await communityService.GetPostAsync(post.Id, userId.Value, cancellationToken);
            return Ok(enriched ?? post);
        });
    }

    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedCommunityFeedResponse>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty ? currentUserService.UserId : (Guid?)null;
        if (userId is null)
        {
            var cacheKey = BuildFeedCacheKey(page, pageSize);
            var cached = await GetCachedAsync<PagedCommunityFeedResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Ok(cached);
            }

            var result = await communityService.GetFeedAsync(page, pageSize, userId, cancellationToken);
            await SetCachedAsync(cacheKey, result, FeedCacheTtl, cancellationToken);
            return Ok(result);
        }

        return Ok(await communityService.GetFeedAsync(page, pageSize, userId, cancellationToken));
    }

    [HttpGet("posts/{postId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CommunityPostDto>> GetPost(Guid postId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty ? currentUserService.UserId : (Guid?)null;
        if (userId is null)
        {
            var cacheKey = BuildPostCacheKey(postId);
            var cached = await GetCachedAsync<CommunityPostDto>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Ok(cached);
            }

            var postResult = await communityService.GetPostAsync(postId, userId, cancellationToken);
            if (postResult is null) return NotFound();
            await SetCachedAsync(cacheKey, postResult, PostCacheTtl, cancellationToken);
            return Ok(postResult);
        }

        var result = await communityService.GetPostAsync(postId, userId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("users/{userId:guid}/posts")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedCommunityFeedResponse>> GetUserPosts(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var viewer = currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty ? currentUserService.UserId : (Guid?)null;
        if (viewer is null)
        {
            var cacheKey = BuildUserPostsCacheKey(userId, page, pageSize);
            var cached = await GetCachedAsync<PagedCommunityFeedResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Ok(cached);
            }

            var result = await communityService.GetUserPostsAsync(userId, page, pageSize, viewer, cancellationToken);
            await SetCachedAsync(cacheKey, result, UserPostsCacheTtl, cancellationToken);
            return Ok(result);
        }

        return Ok(await communityService.GetUserPostsAsync(userId, page, pageSize, viewer, cancellationToken));
    }

    [HttpPut("posts/{postId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromForm] CommunityUpsertPostFormRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var isModerator = IsModerator();
        return await Execute(async () =>
        {
            var existing = await communityDbContext.CommunityPostMedia.Where(x => x.PostId == postId).OrderBy(x => x.Order).ToListAsync(cancellationToken);
            var removeSet = (request.RemoveMediaIds ?? []).ToHashSet();
            var existingIds = existing.Select(x => x.Id).ToHashSet();
            if (!removeSet.IsSubsetOf(existingIds))
            {
                return BadRequest(new { errorCode = "community_media_invalid_reference" });
            }

            var expectedKeepIds = existingIds.Except(removeSet).ToHashSet();
            var keepIds = request.ExistingMediaIds ?? expectedKeepIds.ToList();
            if (keepIds.Count != keepIds.Distinct().Count() || !keepIds.ToHashSet().SetEquals(expectedKeepIds))
            {
                return BadRequest(new { errorCode = "community_media_invalid_reference" });
            }

            var newFiles = request.MediaFiles ?? [];
            if (keepIds.Count + newFiles.Count > 8) return BadRequest(new { errorCode = "community_media_too_many_files" });
            if (string.IsNullOrWhiteSpace(request.Caption) && keepIds.Count + newFiles.Count == 0) return BadRequest(new { errorCode = "community_post_empty" });

            var orderedKeepIds = request.MediaOrder ?? keepIds;
            if (orderedKeepIds.Count != orderedKeepIds.Distinct().Count() || !orderedKeepIds.ToHashSet().SetEquals(expectedKeepIds))
            {
                return BadRequest(new { errorCode = "community_media_order_invalid" });
            }

            IReadOnlyList<CommunityUploadedMedia> uploaded = [];
            try
            {
                if (newFiles.Count > 0)
                {
                    uploaded = await communityMediaService.UploadPostMediaAsync(postId, userId.Value, await ToPayloadsAsync(newFiles, cancellationToken), cancellationToken);
                }

                var result = await communityService.UpdatePostAsync(postId, userId.Value, isModerator, new UpdateCommunityPostRequest(request.Caption, request.City, request.District), cancellationToken);
                if (result is null)
                {
                    await CleanupUploadedMediaAsync(uploaded, cancellationToken);
                    return NotFound();
                }

                var mediaOrder = orderedKeepIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
                foreach (var media in existing)
                {
                    if (removeSet.Contains(media.Id))
                    {
                        media.IsDeleted = true;
                        media.DeletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        media.Order = (short)mediaOrder[media.Id];
                    }
                    media.UpdatedAt = DateTime.UtcNow;
                }

                foreach (var item in uploaded.Select((media, index) => new { media, index }))
                {
                    communityDbContext.CommunityPostMedia.Add(new CommunityPostMedia
                    {
                        PostId = postId,
                        StorageKey = item.media.StorageKey,
                        PublicUrl = item.media.PublicUrl,
                        ContentType = item.media.ContentType,
                        SizeBytes = item.media.SizeBytes,
                        Width = item.media.Width,
                        Height = item.media.Height,
                        Order = (short)(orderedKeepIds.Count + item.index),
                        Status = CommunityMediaStatus.Ready
                    });
                }

                await communityDbContext.SaveChangesAsync(cancellationToken);
                foreach (var media in existing.Where(x => removeSet.Contains(x.Id)))
                {
                    _ = await communityMediaService.DeleteMediaByStorageKeyAsync(media.StorageKey, cancellationToken);
                }

                var refreshed = await communityService.GetPostAsync(postId, userId.Value, cancellationToken);
                return Ok(refreshed ?? result);
            }
            catch
            {
                await CleanupUploadedMediaAsync(uploaded, cancellationToken);
                throw;
            }
        });
    }

    [HttpDelete("posts/{postId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var isModerator = IsModerator();
        return await Execute(async () => (await communityService.DeletePostAsync(postId, userId.Value, isModerator, cancellationToken)) ? NoContent() : NotFound());
    }

    [HttpGet("posts/{postId:guid}/comments")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedCommunityCommentsResponse>> GetPostComments(Guid postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty ? currentUserService.UserId : (Guid?)null;
        if (userId is null)
        {
            var cacheKey = BuildCommentsCacheKey(postId, page, pageSize);
            var cached = await GetCachedAsync<PagedCommunityCommentsResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Ok(cached);
            }

            var commentsResult = await communityService.GetPostCommentsAsync(postId, page, pageSize, userId, cancellationToken);
            if (commentsResult is null) return NotFound();
            await SetCachedAsync(cacheKey, commentsResult, CommentsCacheTtl, cancellationToken);
            return Ok(commentsResult);
        }

        var result = await communityService.GetPostCommentsAsync(postId, page, pageSize, userId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommunityCommentRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var result = await communityService.AddCommentAsync(postId, userId.Value, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        });
    }

    [HttpPost("comments/{commentId:guid}/replies")]
    [Authorize]
    public async Task<IActionResult> Reply(Guid commentId, [FromBody] CreateCommunityReplyRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var result = await communityService.ReplyCommentAsync(commentId, userId.Value, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        });
    }

    [HttpDelete("comments/{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () => (await communityService.DeleteCommentAsync(commentId, userId.Value, IsModerator(), cancellationToken)) ? NoContent() : NotFound());
    }

    [HttpPost("posts/{postId:guid}/like")]
    [Authorize]
    public async Task<ActionResult<ToggleActionResponse>> LikePost(Guid postId, CancellationToken cancellationToken) => await SetPostLike(postId, true, cancellationToken);

    [HttpDelete("posts/{postId:guid}/like")]
    [Authorize]
    public async Task<ActionResult<ToggleActionResponse>> UnlikePost(Guid postId, CancellationToken cancellationToken) => await SetPostLike(postId, false, cancellationToken);

    [HttpPost("comments/{commentId:guid}/like")]
    [Authorize]
    public async Task<ActionResult<ToggleActionResponse>> LikeComment(Guid commentId, CancellationToken cancellationToken) => await SetCommentLike(commentId, true, cancellationToken);

    [HttpDelete("comments/{commentId:guid}/like")]
    [Authorize]
    public async Task<ActionResult<ToggleActionResponse>> UnlikeComment(Guid commentId, CancellationToken cancellationToken) => await SetCommentLike(commentId, false, cancellationToken);

    [HttpPost("posts/{postId:guid}/save")]
    [Authorize]
    public async Task<ActionResult<ToggleActionResponse>> SavePost(Guid postId, CancellationToken cancellationToken) => await SetSaved(postId, true, cancellationToken);

    [HttpDelete("posts/{postId:guid}/save")]
    [Authorize]
    public async Task<ActionResult<ToggleActionResponse>> UnsavePost(Guid postId, CancellationToken cancellationToken) => await SetSaved(postId, false, cancellationToken);

    [HttpPut("posts/{postId:guid}/interest")]
    [Authorize]
    public async Task<IActionResult> SetInterest(Guid postId, [FromBody] SetCommunityPostInterestRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var value = await communityService.SetPostInterestAsync(postId, userId.Value, request, cancellationToken);
            return value is null ? NotFound() : Ok(new { currentUserInterest = value });
        });
    }

    [HttpDelete("posts/{postId:guid}/interest")]
    [Authorize]
    public async Task<IActionResult> ClearInterest(Guid postId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var cleared = await communityService.ClearPostInterestAsync(postId, userId.Value, cancellationToken);
        return cleared ? NoContent() : NotFound();
    }

    [HttpPost("posts/{postId:guid}/reactions")]
    [Authorize]
    public async Task<IActionResult> SetPostReaction(Guid postId, [FromBody] AddOrUpdateCommunityReactionRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var result = await communityService.SetPostReactionAsync(postId, userId.Value, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        });
    }

    [HttpDelete("posts/{postId:guid}/reactions")]
    [Authorize]
    public async Task<IActionResult> RemovePostReaction(Guid postId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var result = await communityService.RemovePostReactionAsync(postId, userId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("comments/{commentId:guid}/reactions")]
    [Authorize]
    public async Task<IActionResult> SetCommentReaction(Guid commentId, [FromBody] AddOrUpdateCommunityReactionRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var result = await communityService.SetCommentReactionAsync(commentId, userId.Value, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        });
    }

    [HttpDelete("comments/{commentId:guid}/reactions")]
    [Authorize]
    public async Task<IActionResult> RemoveCommentReaction(Guid commentId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var result = await communityService.RemoveCommentReactionAsync(commentId, userId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("posts/{postId:guid}/reports")]
    [Authorize]
    public async Task<IActionResult> ReportPost(Guid postId, [FromBody] ReportCommunityPostRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var result = await communityService.ReportPostAsync(postId, userId.Value, request, cancellationToken);
            return result switch { null => NotFound(), false => Conflict(new { errorCode = "report_duplicate" }), _ => Ok() };
        });
    }

    [HttpPost("comments/{commentId:guid}/reports")]
    [Authorize]
    public async Task<IActionResult> ReportComment(Guid commentId, [FromBody] ReportCommunityCommentRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        return await Execute(async () =>
        {
            var result = await communityService.ReportCommentAsync(commentId, userId.Value, request, cancellationToken);
            return result switch { null => NotFound(), false => Conflict(new { errorCode = "report_duplicate" }), _ => Ok() };
        });
    }

    [HttpGet("moderation/reports")]
    [Authorize(Roles = "Admin,Moderator,admin,moderator")]
    public async Task<IActionResult> GetModerationReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
        var skip = (safePage - 1) * safePageSize;

        var postReports = await communityDbContext.CommunityPostReports
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(safePageSize)
            .Select(x => new
            {
                reportId = x.Id,
                type = "post",
                postId = x.PostId,
                reporterUserId = x.ReporterUserId,
                targetOwnerUserId = x.TargetOwnerUserId,
                reason = x.Reason.ToString(),
                description = x.Description,
                createdAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var commentReports = await communityDbContext.CommunityCommentReports
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(safePageSize)
            .Select(x => new
            {
                reportId = x.Id,
                type = "comment",
                commentId = x.CommentId,
                reporterUserId = x.ReporterUserId,
                targetOwnerUserId = x.TargetOwnerUserId,
                reason = x.Reason.ToString(),
                description = x.Description,
                createdAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { page = safePage, pageSize = safePageSize, postReports, commentReports });
    }

    [HttpPost("moderation/posts/{postId:guid}/hide")]
    [Authorize(Roles = "Admin,Moderator,admin,moderator")]
    public async Task<IActionResult> HidePost(Guid postId, CancellationToken cancellationToken)
    {
        var post = await communityDbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return NotFound();
        post.Status = CommunityPostStatus.Hidden;
        post.UpdatedAt = DateTime.UtcNow;
        await communityDbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { hidden = true });
    }

    [HttpPost("moderation/posts/{postId:guid}/restore")]
    [Authorize(Roles = "Admin,Moderator,admin,moderator")]
    public async Task<IActionResult> RestorePost(Guid postId, CancellationToken cancellationToken)
    {
        var post = await communityDbContext.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null) return NotFound();
        post.Status = CommunityPostStatus.Published;
        post.UpdatedAt = DateTime.UtcNow;
        await communityDbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { restored = true });
    }

    [HttpPost("moderation/comments/{commentId:guid}/hide")]
    [Authorize(Roles = "Admin,Moderator,admin,moderator")]
    public async Task<IActionResult> HideComment(Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await communityDbContext.CommunityComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null) return NotFound();
        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        await communityDbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { hidden = true });
    }

    [HttpPost("moderation/comments/{commentId:guid}/restore")]
    [Authorize(Roles = "Admin,Moderator,admin,moderator")]
    public async Task<IActionResult> RestoreComment(Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await communityDbContext.CommunityComments.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null) return NotFound();
        comment.IsDeleted = false;
        comment.DeletedAt = null;
        comment.UpdatedAt = DateTime.UtcNow;
        await communityDbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { restored = true });
    }

    private async Task<ActionResult<ToggleActionResponse>> SetPostLike(Guid postId, bool liked, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var result = await communityService.SetPostLikeAsync(postId, userId.Value, liked, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<ActionResult<ToggleActionResponse>> SetCommentLike(Guid commentId, bool liked, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var result = await communityService.SetCommentLikeAsync(commentId, userId.Value, liked, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<ActionResult<ToggleActionResponse>> SetSaved(Guid postId, bool saved, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null) return Unauthorized();
        var result = await communityService.SetSavedPostAsync(postId, userId.Value, saved, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid? RequireUserId() => currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty ? currentUserService.UserId : null;

    private bool IsModerator() => User.IsInRole("Admin") || User.IsInRole("Moderator") || User.IsInRole("admin") || User.IsInRole("moderator");

    private async Task CleanupUploadedMediaAsync(IEnumerable<CommunityUploadedMedia> uploaded, CancellationToken cancellationToken)
    {
        foreach (var media in uploaded)
        {
            _ = await communityMediaService.DeleteMediaByStorageKeyAsync(media.StorageKey, cancellationToken);
        }
    }

    private async Task TryDeleteCreatedPostAsync(Guid postId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            _ = await communityService.DeletePostAsync(postId, userId, false, cancellationToken);
        }
        catch
        {
            // Preserve the upload exception while making a best-effort rollback.
        }
    }

    private static async Task<List<CommunityMediaUploadPayload>> ToPayloadsAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var list = new List<CommunityMediaUploadPayload>();
        var index = 0;
        foreach (var file in files)
        {
            if (file.Length > 15 * 1024 * 1024)
            {
                throw new ArgumentException("community_media_input_too_large");
            }
            await using var stream = file.OpenReadStream();
            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            list.Add(new CommunityMediaUploadPayload(file.FileName, file.ContentType, memory.ToArray(), index++));
        }

        return list;
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<ActionResult<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { errorCode = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { errorCode = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private async Task<T?> GetCachedAsync<T>(string cacheKey, CancellationToken cancellationToken)
        where T : class
    {
        var cached = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
        return string.IsNullOrWhiteSpace(cached) ? null : JsonSerializer.Deserialize<T>(cached, CacheJsonOptions);
    }

    private async Task SetCachedAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class
    {
        await distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(value, CacheJsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    private static string BuildFeedCacheKey(int page, int pageSize) => $"community:feed:{page}:{pageSize}";
    private static string BuildPostCacheKey(Guid postId) => $"community:post:{postId:D}";
    private static string BuildUserPostsCacheKey(Guid userId, int page, int pageSize) => $"community:user-posts:{userId:D}:{page}:{pageSize}";
    private static string BuildCommentsCacheKey(Guid postId, int page, int pageSize) => $"community:comments:{postId:D}:{page}:{pageSize}";
}
