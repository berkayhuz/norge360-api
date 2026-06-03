// <copyright file="CommunityPostMediaConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityPostMediaConfiguration : IEntityTypeConfiguration<CommunityPostMedia> { [Obsolete] public void Configure(EntityTypeBuilder<CommunityPostMedia> b) { b.ToTable("CommunityPostMedia"); b.HasKey(x => x.Id); b.Property(x => x.StorageKey).HasMaxLength(512).IsRequired(); b.Property(x => x.PublicUrl).HasMaxLength(1024).IsRequired(); b.Property(x => x.ContentType).HasMaxLength(100).IsRequired(); b.Property(x => x.Order).HasDefaultValue((short)0); b.Property(x => x.Status).HasConversion<short>(); b.HasIndex(x => new { x.PostId, x.Order }); b.HasCheckConstraint("CK_CommunityPostMedia_Order_Range", "\"Order\" >= 0 AND \"Order\" <= 7"); b.HasOne(x => x.Post).WithMany(x => x.Media).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade); b.HasQueryFilter(x => !x.IsDeleted); } }
