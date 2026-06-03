// <copyright file="CommunityPostConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost> { public void Configure(EntityTypeBuilder<CommunityPost> b) { b.ToTable("CommunityPosts"); b.HasKey(x => x.Id); b.Property(x => x.Caption).HasMaxLength(2200); b.Property(x => x.City).HasMaxLength(128); b.Property(x => x.District).HasMaxLength(128); b.Property(x => x.Status).HasConversion<short>(); b.HasIndex(x => new { x.UserId, x.CreatedAt }); b.HasIndex(x => x.CreatedAt); b.HasQueryFilter(x => !x.IsDeleted); } }
