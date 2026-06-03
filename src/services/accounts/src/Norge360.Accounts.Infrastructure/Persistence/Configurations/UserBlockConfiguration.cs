// <copyright file="UserBlockConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable(
            "UserBlocks",
            table => table.HasCheckConstraint(
                "CK_UserBlocks_BlockerProfileId_NotEqual_BlockedProfileId",
                "\"BlockerProfileId\" <> \"BlockedProfileId\""));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.BlockerProfileId).IsRequired();
        builder.Property(x => x.BlockedProfileId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.BlockerProfileId, x.BlockedProfileId }).IsUnique();
        builder.HasIndex(x => x.BlockerProfileId);
        builder.HasIndex(x => x.BlockedProfileId);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.BlockerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.BlockedProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
