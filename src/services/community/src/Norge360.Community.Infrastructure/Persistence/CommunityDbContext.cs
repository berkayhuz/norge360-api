// <copyright file="CommunityDbContext.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence; public sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> options) : DbContext(options), ICommunityDbContext { public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>(); public DbSet<CommunityPostMedia> CommunityPostMedia => Set<CommunityPostMedia>(); public DbSet<CommunityComment> CommunityComments => Set<CommunityComment>(); public DbSet<CommunityPostLike> CommunityPostLikes => Set<CommunityPostLike>(); public DbSet<CommunityCommentLike> CommunityCommentLikes => Set<CommunityCommentLike>(); public DbSet<CommunitySavedPost> CommunitySavedPosts => Set<CommunitySavedPost>(); public DbSet<CommunityPostReaction> CommunityPostReactions => Set<CommunityPostReaction>(); public DbSet<CommunityCommentReaction> CommunityCommentReactions => Set<CommunityCommentReaction>(); public DbSet<CommunityPostInterest> CommunityPostInterests => Set<CommunityPostInterest>(); public DbSet<CommunityPostReport> CommunityPostReports => Set<CommunityPostReport>(); public DbSet<CommunityCommentReport> CommunityCommentReports => Set<CommunityCommentReport>(); protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly); }

