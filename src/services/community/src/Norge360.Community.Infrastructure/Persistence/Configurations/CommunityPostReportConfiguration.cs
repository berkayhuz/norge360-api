// <copyright file="CommunityPostReportConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityPostReportConfiguration : IEntityTypeConfiguration<CommunityPostReport> { public void Configure(EntityTypeBuilder<CommunityPostReport> b) { b.ToTable("CommunityPostReports"); b.HasKey(x => x.Id); b.Property(x => x.Reason).HasConversion<short>(); b.Property(x => x.Description).HasMaxLength(1000); b.HasIndex(x => new { x.PostId, x.ReporterUserId }).IsUnique().HasFilter("\"IsDeleted\" = false"); b.HasQueryFilter(x => !x.IsDeleted); } }
