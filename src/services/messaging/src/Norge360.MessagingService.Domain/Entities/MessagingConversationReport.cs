// <copyright file="MessagingConversationReport.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingConversationReport
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid ReporterUserId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public Guid? MessageId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public byte[]? UserProvidedEvidenceCipherText { get; set; }
    public byte[]? UserProvidedEvidenceNonce { get; set; }
    public string? EvidenceKeyId { get; set; }
    public ModerationReportStatus Status { get; set; } = ModerationReportStatus.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
