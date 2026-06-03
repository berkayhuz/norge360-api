// <copyright file="UsernameHistoryConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class UsernameHistoryConfiguration : IEntityTypeConfiguration<UsernameHistory>
{
    public void Configure(EntityTypeBuilder<UsernameHistory> builder)
    {
        builder.ToTable("UsernameHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.OldUsername).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NormalizedOldUsername).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NewUsername).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NormalizedNewUsername).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();
        builder.HasIndex(x => x.ProfileId);
        builder.HasIndex(x => x.NormalizedOldUsername);
        builder.HasIndex(x => x.NormalizedNewUsername);
        builder.HasIndex(x => new { x.NormalizedOldUsername, x.ReleasedAt })
            .HasFilter("\"ReleasedAt\" IS NULL");
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
