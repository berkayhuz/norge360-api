// <copyright file="MessagingResponseMapper.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Contracts.Responses;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Application.Services;

internal static class MessagingResponseMapper
{
    public static ConversationSummaryResponse ToConversationSummary(
        MessagingConversation conversation,
        MessagingConversationParticipant viewer,
        int unreadCount)
    {
        var participants = conversation.Participants
            .Where(static participant => participant.LeftAtUtc is null)
            .OrderBy(static participant => participant.JoinedAtUtc)
            .Select(ToParticipantResponse)
            .ToArray();

        return new ConversationSummaryResponse(
            conversation.Id,
            conversation.Kind,
            conversation.RequestStatus,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            conversation.LastMessageId,
            conversation.LastMessageSenderUserId,
            conversation.LastMessageAtUtc,
            unreadCount,
            viewer.ArchivedAtUtc is not null,
            viewer.MutedUntilUtc is not null && viewer.MutedUntilUtc > DateTimeOffset.UtcNow,
            viewer.PinnedAtUtc is not null,
            viewer.MarkedUnreadAtUtc is not null,
            participants);
    }

    public static ConversationParticipantResponse ToParticipantResponse(MessagingConversationParticipant participant) =>
        new(
            participant.UserId,
            participant.Role,
            participant.JoinedAtUtc,
            participant.LastReadAtUtc,
            participant.LastDeliveredAtUtc,
            participant.NicknameCipherText is null || participant.NicknameNonce is null || string.IsNullOrWhiteSpace(participant.NicknameKeyId)
                ? null
                : new EncryptedTextEnvelopeResponse(
                    participant.NicknameCipherText,
                    participant.NicknameNonce,
                    participant.NicknameKeyId,
                    "xchacha20-poly1305"),
            participant.ThemeKey,
            participant.BackgroundKey,
            participant.NotificationSoundEnabled);

    public static MessageResponse ToMessageResponse(MessagingMessage message, MessagingMessage? replyPreview = null) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.SenderDeviceId,
            message.ClientMessageId,
            message.Kind,
            message.State,
            new EncryptedTextEnvelopeResponse(
                message.CipherText,
                message.CipherNonce,
                message.CipherKeyId,
                message.EncryptionAlgorithm),
            message.Attachments
                .OrderBy(static attachment => attachment.CreatedAtUtc)
                .Select(static attachment => new MessageAttachmentResponse(
                    attachment.Id,
                    attachment.Kind,
                    attachment.StorageKey,
                    attachment.ContentType,
                    attachment.SizeBytes,
                    attachment.Width,
                    attachment.Height,
                    attachment.DurationMs,
                    attachment.WaveformJson,
                    attachment.EncryptedFileKey,
                    attachment.KeyNonce,
                    attachment.KeyId,
                    attachment.ViewOnce))
                .ToArray(),
            message.Reactions
                .OrderBy(static reaction => reaction.CreatedAtUtc)
                .Select(static reaction => new MessageReactionResponse(reaction.UserId, reaction.Emoji, reaction.CreatedAtUtc))
                .ToArray(),
            message.Receipts
                .OrderBy(static receipt => receipt.UserId)
                .Select(static receipt => new MessageReceiptResponse(receipt.UserId, receipt.DeliveredAtUtc, receipt.ReadAtUtc))
                .ToArray(),
            message.ReplyToMessageId,
            replyPreview is null ? null : ToMessageResponse(replyPreview, null),
            message.ForwardedFromConversationId,
            message.ForwardedFromMessageId,
            message.SharedPostId,
            message.IsForwarded,
            message.IsEdited,
            message.IsPinned,
            message.ViewOnce,
            message.ExpiresAtUtc,
            message.CreatedAtUtc,
            message.SentAtUtc,
            message.EditedAtUtc,
            message.RecalledAtUtc,
            message.EditUntilUtc,
            message.RecallUntilUtc,
            message.AssociatedDataJson);
}
