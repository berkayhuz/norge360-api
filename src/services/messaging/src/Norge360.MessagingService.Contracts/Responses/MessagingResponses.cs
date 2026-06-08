// <copyright file="MessagingResponses.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Contracts.Responses;

public sealed record ConversationSummaryResponse(
    Guid Id,
    ConversationKind Kind,
    ConversationRequestStatus RequestStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? LastMessageId,
    Guid? LastMessageSenderUserId,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount,
    bool IsArchived,
    bool IsMuted,
    bool IsPinned,
    bool IsMarkedUnread,
    IReadOnlyCollection<ConversationParticipantResponse> Participants);

public sealed record ConversationPageResponse(
    IReadOnlyCollection<ConversationSummaryResponse> Items,
    int PageSize,
    DateTimeOffset? NextBeforeUtc);

public sealed record ConversationParticipantResponse(
    Guid UserId,
    ConversationParticipantRole Role,
    DateTimeOffset JoinedAtUtc,
    DateTimeOffset? LastReadAtUtc,
    DateTimeOffset? LastDeliveredAtUtc,
    EncryptedTextEnvelopeResponse? Nickname,
    string? ThemeKey,
    string? BackgroundKey,
    bool NotificationSoundEnabled);

public sealed record MessagePageResponse(
    IReadOnlyCollection<MessageResponse> Items,
    int PageSize,
    DateTimeOffset? NextBeforeUtc);

public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDeviceId,
    string ClientMessageId,
    MessageKind Kind,
    MessageDeliveryState State,
    EncryptedTextEnvelopeResponse Body,
    IReadOnlyCollection<MessageAttachmentResponse> Attachments,
    IReadOnlyCollection<MessageReactionResponse> Reactions,
    IReadOnlyCollection<MessageReceiptResponse> Receipts,
    Guid? ReplyToMessageId,
    MessageResponse? ReplyPreview,
    Guid? ForwardedFromConversationId,
    Guid? ForwardedFromMessageId,
    Guid? SharedPostId,
    bool IsForwarded,
    bool IsEdited,
    bool IsPinned,
    bool ViewOnce,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? EditedAtUtc,
    DateTimeOffset? RecalledAtUtc,
    DateTimeOffset EditUntilUtc,
    DateTimeOffset RecallUntilUtc,
    string? AssociatedDataJson);

public sealed record MessageAttachmentResponse(
    Guid Id,
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

public sealed record MessageReactionResponse(Guid UserId, string Emoji, DateTimeOffset CreatedAtUtc);

public sealed record MessageReceiptResponse(Guid UserId, DateTimeOffset? DeliveredAtUtc, DateTimeOffset? ReadAtUtc);

public sealed record EncryptedTextEnvelopeResponse(
    byte[] CipherText,
    byte[] CipherNonce,
    string CipherKeyId,
    string EncryptionAlgorithm);

public sealed record MessagingSettingsResponse(
    MessagingPermission MessagePermission,
    MessagingGroupInvitePermission GroupInvitePermission,
    MessagingOnlineVisibility OnlineVisibility,
    bool ReadReceiptsEnabled,
    bool TypingIndicatorsEnabled,
    bool LinkPreviewsEnabled,
    bool ShowMessagePreviewInNotifications,
    DateTimeOffset UpdatedAtUtc);

public sealed record MessagingDeviceResponse(
    Guid Id,
    string DeviceId,
    string PublicIdentityKey,
    string SignedPreKey,
    string SignedPreKeySignature,
    string? OneTimePreKeysJson,
    string SupportedAlgorithms,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RevokedAtUtc);

public sealed record MessagingDeviceKeyBundleResponse(
    Guid UserId,
    string DeviceId,
    string PublicIdentityKey,
    string SignedPreKey,
    string SignedPreKeySignature,
    string? OneTimePreKeysJson,
    string SupportedAlgorithms,
    DateTimeOffset CreatedAtUtc);

public sealed record MessagingReportResponse(Guid Id, ModerationReportStatus Status, DateTimeOffset CreatedAtUtc);
