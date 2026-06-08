// <copyright file="MessagingRulesOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Application.Options;

public sealed class MessagingRulesOptions
{
    public const string SectionName = "Messaging:Rules";

    public int EditWindowSeconds { get; set; } = 600;
    public int RecallWindowSeconds { get; set; } = 600;
    public int MaxPageSize { get; set; } = 100;
    public int MaxGroupParticipants { get; set; } = 250;
    public int MaxBulkRecipients { get; set; } = 50;
    public int MaxMessageCipherTextBytes { get; set; } = 64 * 1024;
    public int MaxAttachmentCount { get; set; } = 10;
}
