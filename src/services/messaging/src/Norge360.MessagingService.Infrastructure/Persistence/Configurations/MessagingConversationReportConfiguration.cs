// <copyright file="MessagingConversationReportConfiguration.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence.Configurations;

public sealed class MessagingConversationReportConfiguration : IEntityTypeConfiguration<MessagingConversationReport>
{
    public void Configure(EntityTypeBuilder<MessagingConversationReport> builder)
    {
        builder.ToTable("MessagingConversationReports");
        builder.HasKey(static report => report.Id);
        builder.Property(static report => report.ReasonCode).HasMaxLength(64).IsRequired();
        builder.Property(static report => report.UserProvidedEvidenceCipherText).HasColumnType("bytea");
        builder.Property(static report => report.UserProvidedEvidenceNonce).HasColumnType("bytea");
        builder.Property(static report => report.EvidenceKeyId).HasMaxLength(128);
        builder.Property(static report => report.Status).IsRequired();
        builder.Property(static report => report.CreatedAtUtc).IsRequired();
        builder.HasIndex(static report => new { report.ConversationId, report.CreatedAtUtc });
        builder.HasIndex(static report => new { report.ReporterUserId, report.CreatedAtUtc });
        builder.HasIndex(static report => report.Status);
    }
}
