// <copyright file="CommunityCommentConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityCommentConfiguration : IEntityTypeConfiguration<CommunityComment> { public void Configure(EntityTypeBuilder<CommunityComment> b) { b.ToTable("CommunityComments"); b.HasKey(x => x.Id); b.Property(x => x.Slug).HasMaxLength(20).IsRequired(); b.HasIndex(x => x.Slug).IsUnique(); b.Property(x => x.Body).HasMaxLength(1000).IsRequired(); b.HasIndex(x => new { x.PostId, x.ParentCommentId, x.CreatedAt }); b.HasOne(x => x.Post).WithMany(x => x.Comments).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade); b.HasOne(x => x.ParentComment).WithMany(x => x.Replies).HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Restrict); b.HasQueryFilter(x => !x.IsDeleted); } }
