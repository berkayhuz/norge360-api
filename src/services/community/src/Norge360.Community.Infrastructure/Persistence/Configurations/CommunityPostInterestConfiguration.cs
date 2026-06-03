// <copyright file="CommunityPostInterestConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Community.Domain.Entities;
namespace Norge360.Community.Infrastructure.Persistence.Configurations; public sealed class CommunityPostInterestConfiguration : IEntityTypeConfiguration<CommunityPostInterest> { public void Configure(EntityTypeBuilder<CommunityPostInterest> b) { b.ToTable("CommunityPostInterests"); b.HasKey(x => x.Id); b.Property(x => x.InterestType).HasConversion<short>(); b.HasIndex(x => new { x.PostId, x.UserId }).IsUnique().HasFilter("\"IsDeleted\" = false"); b.HasQueryFilter(x => !x.IsDeleted); } }
