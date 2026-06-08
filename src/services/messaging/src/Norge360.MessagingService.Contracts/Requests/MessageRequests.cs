// <copyright file="MessageRequests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Contracts.Requests;

public sealed record SendEncryptedMessageRequest(
    string ClientMessageId,
    string SenderDeviceId,
    MessageKind Kind,
    EncryptedTextEnvelope Body,
    IReadOnlyCollection<CreateMessageAttachmentRequest> Attachments,
    Guid? ReplyToMessageId,
    Guid? ForwardedFromConversationId,
    Guid? ForwardedFromMessageId,
    Guid? SharedPostId,
    bool ViewOnce,
    int? DisappearingTtlSeconds,
    string? AssociatedDataJson,
    string? ClientSearchTokenHash);

public sealed record EditEncryptedMessageRequest(
    EncryptedTextEnvelope Body,
    string? AssociatedDataJson,
    string? ClientSearchTokenHash);

public sealed record ForwardEncryptedMessageRequest(
    Guid SourceConversationId,
    Guid SourceMessageId,
    IReadOnlyCollection<Guid> TargetConversationIds,
    SendEncryptedMessageRequest Message);

public sealed record BulkEncryptedMessageRequest(
    IReadOnlyCollection<Guid> TargetUserIds,
    SendEncryptedMessageRequest Message);

public sealed record CreateMessageAttachmentRequest(
    MessageAttachmentKind Kind,
    string StorageKey,
    string ContentType,
    long SizeBytes,
    int? Width,
    int? Height,
    int? DurationMs,
    string? WaveformJson,
    byte[] EncryptedFileKey,
    byte[] KeyNonce,
    string KeyId,
    bool ViewOnce);

public sealed record EncryptedTextEnvelope(
    byte[] CipherText,
    byte[] CipherNonce,
    string CipherKeyId,
    string EncryptionAlgorithm);

public sealed record MessageReactionRequest(string Emoji);

public sealed record MarkConversationReadRequest(Guid? LastReadMessageId);

public sealed record TypingSignalRequest(Guid ConversationId, bool IsTyping);

public sealed record SearchConversationMessagesRequest(
    Guid ConversationId,
    IReadOnlyCollection<string> ClientSearchTokenHashes,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    MessageAttachmentKind? AttachmentKind,
    Guid? SenderUserId,
    int PageSize,
    DateTimeOffset? BeforeUtc);
