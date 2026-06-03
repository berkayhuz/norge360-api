// <copyright file="CommunityCommentReactionConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityCommentReactionConfiguration : IEntityTypeConfiguration<CommunityCommentReaction> { public void Configure(EntityTypeBuilder<CommunityCommentReaction> b) { b.ToTable("CommunityCommentReactions"); b.HasKey(x => x.Id); b.Property(x => x.Emoji).HasMaxLength(32).IsRequired(); b.Property(x => x.EmojiCode).HasMaxLength(64).IsRequired(); b.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique().HasFilter("\"IsDeleted\" = false"); b.HasQueryFilter(x => !x.IsDeleted); } }
