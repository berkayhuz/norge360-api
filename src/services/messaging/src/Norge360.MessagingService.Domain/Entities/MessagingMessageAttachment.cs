// <copyright file="MessagingMessageAttachment.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingMessageAttachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public MessageAttachmentKind Kind { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }
    public string? WaveformJson { get; set; }
    public byte[] EncryptedFileKey { get; set; } = [];
    public byte[] KeyNonce { get; set; } = [];
    public string KeyId { get; set; } = string.Empty;
    public bool ViewOnce { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public MessagingMessage? Message { get; set; }
}
