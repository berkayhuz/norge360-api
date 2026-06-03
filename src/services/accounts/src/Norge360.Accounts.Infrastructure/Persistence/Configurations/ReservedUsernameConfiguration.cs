// <copyright file="ReservedUsernameConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class ReservedUsernameConfiguration : IEntityTypeConfiguration<ReservedUsername>
{
    public void Configure(EntityTypeBuilder<ReservedUsername> builder)
    {
        builder.ToTable("ReservedUsernames");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NormalizedValue).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(256);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.HasIndex(x => x.NormalizedValue)
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}
