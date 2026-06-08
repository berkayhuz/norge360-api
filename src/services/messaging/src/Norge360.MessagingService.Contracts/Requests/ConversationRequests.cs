// <copyright file="ConversationRequests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Contracts.Requests;

public sealed record CreateDirectConversationRequest(
    Guid TargetUserId,
    SendEncryptedMessageRequest? InitialMessage);

public sealed record CreateGroupConversationRequest(
    IReadOnlyCollection<Guid> ParticipantUserIds,
    int? DefaultDisappearingTtlSeconds,
    SendEncryptedMessageRequest? InitialMessage);

public sealed record UpdateConversationParticipantRequest(
    DateTimeOffset? MutedUntilUtc,
    DateTimeOffset? ArchivedAtUtc,
    DateTimeOffset? PinnedAtUtc,
    bool MarkUnread,
    DateTimeOffset? ClearedAtUtc,
    bool? NotificationSoundEnabled,
    EncryptedTextEnvelope? Nickname,
    string? ThemeKey,
    string? BackgroundKey);

public sealed record UpdateMessagingSettingsRequest(
    MessagingPermission MessagePermission,
    MessagingGroupInvitePermission GroupInvitePermission,
    MessagingOnlineVisibility OnlineVisibility,
    bool ReadReceiptsEnabled,
    bool TypingIndicatorsEnabled,
    bool LinkPreviewsEnabled,
    bool ShowMessagePreviewInNotifications);

public sealed record RegisterMessagingDeviceRequest(
    string DeviceId,
    string PublicIdentityKey,
    string SignedPreKey,
    string SignedPreKeySignature,
    string? OneTimePreKeysJson,
    string SupportedAlgorithms);

public sealed record ReportConversationRequest(
    Guid? ReportedUserId,
    Guid? MessageId,
    string ReasonCode,
    string? MetadataJson,
    EncryptedTextEnvelope? UserProvidedEvidence);

public sealed record AcceptMessageRequestRequest(Guid ConversationId);

public sealed record RejectMessageRequestRequest(Guid ConversationId, bool BlockSender);
