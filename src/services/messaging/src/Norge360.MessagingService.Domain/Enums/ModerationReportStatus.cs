// <copyright file="ModerationReportStatus.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Enums;

public enum ModerationReportStatus
{
    Pending = 1,
    Triaged = 2,
    Actioned = 3,
    Rejected = 4
}
