// <copyright file="UserProfileReportConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Infrastructure.Persistence.Configurations;

public sealed class UserProfileReportConfiguration : IEntityTypeConfiguration<UserProfileReport>
{
    public void Configure(EntityTypeBuilder<UserProfileReport> builder)
    {
        builder.ToTable(
            "UserProfileReports",
            table => table.HasCheckConstraint(
                "CK_UserProfileReports_ReporterProfileId_NotEqual_ReportedProfileId",
                "\"ReporterProfileId\" <> \"ReportedProfileId\""));

        builder.HasKey(report => report.Id);
        builder.Property(report => report.ReporterProfileId).IsRequired();
        builder.Property(report => report.ReportedProfileId).IsRequired();
        builder.Property(report => report.ReporterAuthUserId).IsRequired();
        builder.Property(report => report.ReportedAuthUserId).IsRequired();
        builder.Property(report => report.ReasonCode).IsRequired().HasMaxLength(64);
        builder.Property(report => report.Description).HasMaxLength(2_000);
        builder.Property(report => report.CreatedAt).IsRequired();

        builder.HasIndex(report => new { report.ReportedProfileId, report.CreatedAt });
        builder.HasIndex(report => new { report.ReporterProfileId, report.CreatedAt });
        builder.HasIndex(report => report.ReasonCode);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(report => report.ReporterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(report => report.ReportedProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
