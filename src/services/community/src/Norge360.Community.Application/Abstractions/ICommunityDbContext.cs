// <copyright file="ICommunityDbContext.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Community.Domain.Entities;

namespace Norge360.Community.Application.Abstractions;

public interface ICommunityDbContext : ICommunityUnitOfWork
{
    DbSet<CommunityPost> CommunityPosts { get; }
    DbSet<CommunityPostMedia> CommunityPostMedia { get; }
    DbSet<CommunityComment> CommunityComments { get; }
    DbSet<CommunityPostLike> CommunityPostLikes { get; }
    DbSet<CommunityCommentLike> CommunityCommentLikes { get; }
    DbSet<CommunitySavedPost> CommunitySavedPosts { get; }
    DbSet<CommunityPostReaction> CommunityPostReactions { get; }
    DbSet<CommunityCommentReaction> CommunityCommentReactions { get; }
    DbSet<CommunityPostInterest> CommunityPostInterests { get; }
    DbSet<CommunityPostReport> CommunityPostReports { get; }
    DbSet<CommunityCommentReport> CommunityCommentReports { get; }
}
