// <copyright file="MessagingConversationParticipant.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingConversationParticipant
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public ConversationParticipantRole Role { get; set; } = ConversationParticipantRole.Member;
    public DateTimeOffset JoinedAtUtc { get; set; }
    public DateTimeOffset? LeftAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
    public DateTimeOffset? MutedUntilUtc { get; set; }
    public DateTimeOffset? PinnedAtUtc { get; set; }
    public DateTimeOffset? MarkedUnreadAtUtc { get; set; }
    public DateTimeOffset? ClearedAtUtc { get; set; }
    public bool NotificationSoundEnabled { get; set; } = true;
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset? LastReadAtUtc { get; set; }
    public Guid? LastDeliveredMessageId { get; set; }
    public DateTimeOffset? LastDeliveredAtUtc { get; set; }
    public byte[]? NicknameCipherText { get; set; }
    public byte[]? NicknameNonce { get; set; }
    public string? NicknameKeyId { get; set; }
    public string? ThemeKey { get; set; }
    public string? BackgroundKey { get; set; }
    public bool IsActive => LeftAtUtc is null && DeletedAtUtc is null;

    public MessagingConversation? Conversation { get; set; }
}
