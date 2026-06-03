// <copyright file="CommunityCommentLikeConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityCommentLikeConfiguration : IEntityTypeConfiguration<CommunityCommentLike> { public void Configure(EntityTypeBuilder<CommunityCommentLike> b) { b.ToTable("CommunityCommentLikes"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique().HasFilter("\"IsDeleted\" = false"); b.HasQueryFilter(x => !x.IsDeleted); } }
