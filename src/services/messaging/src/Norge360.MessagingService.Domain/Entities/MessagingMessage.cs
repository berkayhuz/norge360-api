// <copyright file="MessagingMessage.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderDeviceId { get; set; } = string.Empty;
    public string ClientMessageId { get; set; } = string.Empty;
    public MessageKind Kind { get; set; }
    public MessageDeliveryState State { get; set; } = MessageDeliveryState.Sent;
    public byte[] CipherText { get; set; } = [];
    public byte[] CipherNonce { get; set; } = [];
    public string CipherKeyId { get; set; } = string.Empty;
    public string EncryptionAlgorithm { get; set; } = "signal-x3dh-double-ratchet/xchacha20-poly1305";
    public string? AssociatedDataJson { get; set; }
    public string? ClientSearchTokenHash { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public Guid? ForwardedFromConversationId { get; set; }
    public Guid? ForwardedFromMessageId { get; set; }
    public Guid? SharedPostId { get; set; }
    public bool IsForwarded { get; set; }
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }
    public bool ViewOnce { get; set; }
    public int? DisappearingTtlSeconds { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? FirstViewedAtUtc { get; set; }
    public DateTimeOffset EditUntilUtc { get; set; }
    public DateTimeOffset RecallUntilUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset SentAtUtc { get; set; }
    public DateTimeOffset? EditedAtUtc { get; set; }
    public DateTimeOffset? RecalledAtUtc { get; set; }
    public int AttachmentCount { get; set; }

    public MessagingConversation? Conversation { get; set; }
    public ICollection<MessagingMessageAttachment> Attachments { get; } = new List<MessagingMessageAttachment>();
    public ICollection<MessagingMessageReaction> Reactions { get; } = new List<MessagingMessageReaction>();
    public ICollection<MessagingMessageReceipt> Receipts { get; } = new List<MessagingMessageReceipt>();
}
