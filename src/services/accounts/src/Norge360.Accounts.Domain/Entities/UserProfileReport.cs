// <copyright file="UserProfileReport.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Domain.Entities;

public sealed class UserProfileReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterProfileId { get; set; }
    public Guid ReportedProfileId { get; set; }
    public Guid ReporterAuthUserId { get; set; }
    public Guid ReportedAuthUserId { get; set; }
    public string ReasonCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
