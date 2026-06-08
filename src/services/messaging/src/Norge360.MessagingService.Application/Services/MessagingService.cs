// <copyright file="MessagingService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Norge360.Clock;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Application.Models;
using Norge360.MessagingService.Application.Options;
using Norge360.MessagingService.Contracts.Requests;
using Norge360.MessagingService.Contracts.Responses;
using Norge360.MessagingService.Domain.Entities;
using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Application.Services;

public sealed class MessagingService(
    IMessagingDbContext dbContext,
    IClock clock,
    IOptions<MessagingRulesOptions> options,
    IMessagingRealtimePublisher realtimePublisher,
    IUserRelationshipReader relationshipReader) : IMessagingService
{
    private readonly MessagingRulesOptions _rules = options.Value;

    public async Task<MessagingOperationResult<ConversationPageResponse>> ListConversationsAsync(
        Guid userId,
        int pageSize,
        DateTimeOffset? beforeUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<ConversationPageResponse>.Unauthorized("authenticated_user_required");
        }

        var safePageSize = SafePageSize(pageSize);
        var participantsQuery = dbContext.ConversationParticipants
            .AsNoTracking()
            .Include(static participant => participant.Conversation!)
            .Where(participant =>
                participant.UserId == userId &&
                participant.DeletedAtUtc == null &&
                participant.Conversation != null &&
                participant.Conversation.Status != ConversationStatus.Deleted);

        if (beforeUtc is not null)
        {
            participantsQuery = participantsQuery.Where(participant =>
                (participant.Conversation!.LastMessageAtUtc ?? participant.Conversation.UpdatedAtUtc) < beforeUtc.Value);
        }

        var participants = await participantsQuery
            .OrderByDescending(static participant => participant.PinnedAtUtc.HasValue)
            .ThenByDescending(static participant => participant.Conversation!.LastMessageAtUtc ?? participant.Conversation.UpdatedAtUtc)
            .Take(safePageSize + 1)
            .ToListAsync(cancellationToken);

        var pageItems = DeduplicateConversationParticipants(participants)
            .Take(safePageSize)
            .ToArray();
        await PopulateConversationParticipantsAsync(pageItems, cancellationToken);

        var unreadCounts = await CountUnreadMessagesAsync(userId, pageItems, cancellationToken);
        var items = pageItems
            .Select(participant => MessagingResponseMapper.ToConversationSummary(
                participant.Conversation!,
                participant,
                unreadCounts.GetValueOrDefault(participant.ConversationId)))
            .ToArray();

        var nextBefore = participants.Count > safePageSize
            ? items.LastOrDefault()?.LastMessageAtUtc ?? items.LastOrDefault()?.UpdatedAtUtc
            : null;

        return MessagingOperationResult<ConversationPageResponse>.Success(new ConversationPageResponse(items, safePageSize, nextBefore));
    }

    private async Task PopulateConversationParticipantsAsync(
        IReadOnlyCollection<MessagingConversationParticipant> viewerParticipants,
        CancellationToken cancellationToken)
    {
        if (viewerParticipants.Count == 0)
        {
            return;
        }

        var conversationIds = viewerParticipants
            .Select(static participant => participant.ConversationId)
            .Distinct()
            .ToArray();
        var participantsByConversationId = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(participant => conversationIds.Contains(participant.ConversationId))
            .GroupBy(static participant => participant.ConversationId)
            .ToDictionaryAsync(static group => group.Key, static group => group.ToArray(), cancellationToken);

        foreach (var viewerParticipant in viewerParticipants)
        {
            if (viewerParticipant.Conversation is null ||
                !participantsByConversationId.TryGetValue(viewerParticipant.ConversationId, out var conversationParticipants))
            {
                continue;
            }

            viewerParticipant.Conversation.Participants.Clear();
            foreach (var conversationParticipant in conversationParticipants)
            {
                viewerParticipant.Conversation.Participants.Add(conversationParticipant);
            }
        }
    }

    public async Task<MessagingOperationResult<MessagePageResponse>> ListMessagesAsync(
        Guid userId,
        Guid conversationId,
        int pageSize,
        DateTimeOffset? beforeUtc,
        CancellationToken cancellationToken = default)
    {
        var access = await LoadParticipantAsync(userId, conversationId, track: false, cancellationToken);
        if (access.Status != MessagingOperationStatus.Success)
        {
            return MessagingOperationResult<MessagePageResponse>.NotFound(access.ErrorCode ?? "conversation_not_found");
        }

        var participant = access.Value!;
        var now = clock.UtcNow;
        var safePageSize = SafePageSize(pageSize);
        var messagesQuery = dbContext.Messages
            .AsNoTracking()
            .Include(static message => message.Attachments)
            .Include(static message => message.Reactions)
            .Include(static message => message.Receipts)
            .Where(message =>
                message.ConversationId == conversationId &&
                (message.ExpiresAtUtc == null || message.ExpiresAtUtc > now) &&
                (participant.ClearedAtUtc == null || message.CreatedAtUtc > participant.ClearedAtUtc));

        if (beforeUtc is not null)
        {
            messagesQuery = messagesQuery.Where(message => message.CreatedAtUtc < beforeUtc.Value);
        }

        var messages = await messagesQuery
            .OrderByDescending(static message => message.CreatedAtUtc)
            .Take(safePageSize + 1)
            .ToListAsync(cancellationToken);

        var pageItems = messages.Take(safePageSize).ToArray();
        var replyPreviews = await LoadReplyPreviewsAsync(pageItems, cancellationToken);
        var responseItems = pageItems
            .Select(message => MessagingResponseMapper.ToMessageResponse(message, replyPreviews.GetValueOrDefault(message.ReplyToMessageId ?? Guid.Empty)))
            .ToArray();

        var nextBefore = messages.Count > safePageSize ? responseItems.LastOrDefault()?.CreatedAtUtc : null;
        return MessagingOperationResult<MessagePageResponse>.Success(new MessagePageResponse(responseItems, safePageSize, nextBefore));
    }

    public async Task<MessagingOperationResult<ConversationSummaryResponse>> CreateDirectConversationAsync(
        Guid userId,
        CreateDirectConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.Unauthorized("authenticated_user_required");
        }

        if (request.TargetUserId == Guid.Empty || request.TargetUserId == userId)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.ValidationFailed("invalid_direct_recipient");
        }

        var privacyDecision = await ResolveDirectPrivacyAsync(userId, request.TargetUserId, cancellationToken);
        if (privacyDecision.Status == MessagingOperationStatus.Forbidden)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.Forbidden(privacyDecision.ErrorCode ?? "messaging_not_allowed");
        }

        var existing = await dbContext.Conversations
            .Include(static conversation => conversation.Participants)
            .Where(conversation =>
                conversation.Kind == ConversationKind.Direct &&
                conversation.Status != ConversationStatus.Deleted &&
                conversation.Participants.Any(participant => participant.UserId == userId && participant.DeletedAtUtc == null) &&
                conversation.Participants.Any(participant => participant.UserId == request.TargetUserId && participant.DeletedAtUtc == null))
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var viewer = existing.Participants.First(participant => participant.UserId == userId);
            var unread = await CountUnreadMessagesAsync(userId, [viewer], cancellationToken);
            return MessagingOperationResult<ConversationSummaryResponse>.Success(
                MessagingResponseMapper.ToConversationSummary(existing, viewer, unread.GetValueOrDefault(existing.Id)));
        }

        var now = clock.UtcNow;
        var conversation = new MessagingConversation
        {
            Id = Guid.NewGuid(),
            Kind = ConversationKind.Direct,
            RequestStatus = privacyDecision.Status == MessagingOperationStatus.Conflict
                ? ConversationRequestStatus.Pending
                : ConversationRequestStatus.Accepted,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        conversation.Participants.Add(CreateParticipant(conversation.Id, userId, ConversationParticipantRole.Owner, now));
        conversation.Participants.Add(CreateParticipant(conversation.Id, request.TargetUserId, ConversationParticipantRole.Member, now));
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.InitialMessage is not null)
        {
            var sendResult = await SendMessageAsync(userId, conversation.Id, request.InitialMessage, cancellationToken);
            if (!sendResult.Succeeded)
            {
                return MessagingOperationResult<ConversationSummaryResponse>.Conflict(sendResult.ErrorCode ?? "initial_message_failed");
            }
        }

        var reloaded = await LoadConversationAsync(conversation.Id, track: false, cancellationToken);
        var viewerParticipant = reloaded!.Participants.First(participant => participant.UserId == userId);
        return MessagingOperationResult<ConversationSummaryResponse>.Success(
            MessagingResponseMapper.ToConversationSummary(reloaded, viewerParticipant, 0));
    }

    public async Task<MessagingOperationResult<ConversationSummaryResponse>> CreateGroupConversationAsync(
        Guid userId,
        CreateGroupConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.Unauthorized("authenticated_user_required");
        }

        var participants = request.ParticipantUserIds
            .Where(static id => id != Guid.Empty)
            .Append(userId)
            .Distinct()
            .ToArray();

        if (participants.Length < 2)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.ValidationFailed("group_requires_participants");
        }

        if (participants.Length > _rules.MaxGroupParticipants)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.ValidationFailed("group_participant_limit_exceeded");
        }

        foreach (var participantUserId in participants.Where(id => id != userId))
        {
            var decision = await ResolveGroupInvitePrivacyAsync(userId, participantUserId, cancellationToken);
            if (decision.Status == MessagingOperationStatus.Forbidden)
            {
                return MessagingOperationResult<ConversationSummaryResponse>.Forbidden(decision.ErrorCode ?? "group_invite_not_allowed");
            }
        }

        var now = clock.UtcNow;
        var conversation = new MessagingConversation
        {
            Id = Guid.NewGuid(),
            Kind = ConversationKind.Group,
            RequestStatus = ConversationRequestStatus.Accepted,
            CreatedByUserId = userId,
            DefaultDisappearingTtlSeconds = request.DefaultDisappearingTtlSeconds,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var participantUserId in participants)
        {
            conversation.Participants.Add(CreateParticipant(
                conversation.Id,
                participantUserId,
                participantUserId == userId ? ConversationParticipantRole.Owner : ConversationParticipantRole.Member,
                now));
        }

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.InitialMessage is not null)
        {
            var sendResult = await SendMessageAsync(userId, conversation.Id, request.InitialMessage, cancellationToken);
            if (!sendResult.Succeeded)
            {
                return MessagingOperationResult<ConversationSummaryResponse>.Conflict(sendResult.ErrorCode ?? "initial_message_failed");
            }
        }

        var reloaded = await LoadConversationAsync(conversation.Id, track: false, cancellationToken);
        var viewer = reloaded!.Participants.First(participant => participant.UserId == userId);
        return MessagingOperationResult<ConversationSummaryResponse>.Success(
            MessagingResponseMapper.ToConversationSummary(reloaded, viewer, 0));
    }

    public async Task<MessagingOperationResult<MessageResponse>> SendMessageAsync(
        Guid userId,
        Guid conversationId,
        SendEncryptedMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<MessageResponse>.Unauthorized("authenticated_user_required");
        }

        var validation = ValidateEncryptedMessage(request);
        if (validation is not null)
        {
            return MessagingOperationResult<MessageResponse>.ValidationFailed(validation);
        }

        var conversation = await LoadConversationAsync(conversationId, track: true, cancellationToken);
        if (conversation is null)
        {
            return MessagingOperationResult<MessageResponse>.NotFound("conversation_not_found");
        }

        var participant = conversation.Participants.FirstOrDefault(item => item.UserId == userId && item.IsActive);
        if (participant is null)
        {
            return MessagingOperationResult<MessageResponse>.Forbidden("conversation_access_denied");
        }

        var existingMessage = await dbContext.Messages
            .Include(static message => message.Attachments)
            .Include(static message => message.Reactions)
            .Include(static message => message.Receipts)
            .FirstOrDefaultAsync(message =>
                message.ConversationId == conversationId &&
                message.SenderUserId == userId &&
                message.ClientMessageId == request.ClientMessageId,
                cancellationToken);

        if (existingMessage is not null)
        {
            var existingReplyPreviews = await LoadReplyPreviewsAsync([existingMessage], cancellationToken);
            return MessagingOperationResult<MessageResponse>.Success(
                MessagingResponseMapper.ToMessageResponse(
                    existingMessage,
                    existingReplyPreviews.GetValueOrDefault(existingMessage.ReplyToMessageId ?? Guid.Empty)));
        }

        if (request.ReplyToMessageId is not null)
        {
            var replyExists = await dbContext.Messages.AnyAsync(message =>
                message.Id == request.ReplyToMessageId &&
                message.ConversationId == conversationId,
                cancellationToken);

            if (!replyExists)
            {
                return MessagingOperationResult<MessageResponse>.ValidationFailed("reply_message_not_found");
            }
        }

        var now = clock.UtcNow;
        var ttlSeconds = request.DisappearingTtlSeconds ?? conversation.DefaultDisappearingTtlSeconds;
        var message = new MessagingMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = userId,
            SenderDeviceId = request.SenderDeviceId.Trim(),
            ClientMessageId = request.ClientMessageId.Trim(),
            Kind = request.Kind,
            State = MessageDeliveryState.Sent,
            CipherText = request.Body.CipherText,
            CipherNonce = request.Body.CipherNonce,
            CipherKeyId = request.Body.CipherKeyId.Trim(),
            EncryptionAlgorithm = NormalizeAlgorithm(request.Body.EncryptionAlgorithm),
            AssociatedDataJson = NormalizeOptionalJson(request.AssociatedDataJson),
            ClientSearchTokenHash = NormalizeSearchToken(request.ClientSearchTokenHash),
            ReplyToMessageId = request.ReplyToMessageId,
            ForwardedFromConversationId = request.ForwardedFromConversationId,
            ForwardedFromMessageId = request.ForwardedFromMessageId,
            SharedPostId = request.SharedPostId,
            IsForwarded = request.ForwardedFromMessageId is not null || request.ForwardedFromConversationId is not null,
            ViewOnce = request.ViewOnce,
            DisappearingTtlSeconds = ttlSeconds,
            ExpiresAtUtc = ttlSeconds is > 0 ? now.AddSeconds(ttlSeconds.Value) : null,
            EditUntilUtc = now.AddSeconds(_rules.EditWindowSeconds),
            RecallUntilUtc = now.AddSeconds(_rules.RecallWindowSeconds),
            CreatedAtUtc = now,
            SentAtUtc = now,
            AttachmentCount = request.Attachments.Count
        };

        foreach (var attachment in request.Attachments)
        {
            message.Attachments.Add(new MessagingMessageAttachment
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Kind = attachment.Kind,
                StorageKey = attachment.StorageKey.Trim(),
                ContentType = attachment.ContentType.Trim(),
                SizeBytes = attachment.SizeBytes,
                Width = attachment.Width,
                Height = attachment.Height,
                DurationMs = attachment.DurationMs,
                WaveformJson = NormalizeOptionalJson(attachment.WaveformJson),
                EncryptedFileKey = attachment.EncryptedFileKey,
                KeyNonce = attachment.KeyNonce,
                KeyId = attachment.KeyId.Trim(),
                ViewOnce = attachment.ViewOnce || request.ViewOnce,
                CreatedAtUtc = now
            });
        }

        foreach (var recipient in conversation.Participants.Where(item => item.UserId != userId && item.IsActive))
        {
            message.Receipts.Add(new MessagingMessageReceipt
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                UserId = recipient.UserId
            });
        }

        dbContext.Messages.Add(message);
        conversation.LastMessageId = message.Id;
        conversation.LastMessageSenderUserId = userId;
        conversation.LastMessageAtUtc = now;
        conversation.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        var replyPreviews = await LoadReplyPreviewsAsync([message], cancellationToken);
        var response = MessagingResponseMapper.ToMessageResponse(
            message,
            replyPreviews.GetValueOrDefault(message.ReplyToMessageId ?? Guid.Empty));
        await realtimePublisher.MessageCreatedAsync(conversationId, response, cancellationToken);

        return MessagingOperationResult<MessageResponse>.Success(response);
    }

    public async Task<MessagingOperationResult<MessageResponse>> EditMessageAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        EditEncryptedMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureMessageOwnerAsync(userId, conversationId, messageId, cancellationToken);
        if (!access.Succeeded)
        {
            return MessagingOperationResult<MessageResponse>.NotFound(access.ErrorCode ?? "message_not_found");
        }

        var message = access.Value!;
        var now = clock.UtcNow;
        if (message.RecalledAtUtc is not null || message.State == MessageDeliveryState.Recalled)
        {
            return MessagingOperationResult<MessageResponse>.Conflict("message_recalled");
        }

        if (now > message.EditUntilUtc)
        {
            return MessagingOperationResult<MessageResponse>.Expired("message_edit_window_expired");
        }

        if (!IsValidEnvelope(request.Body))
        {
            return MessagingOperationResult<MessageResponse>.ValidationFailed("invalid_encrypted_body");
        }

        message.CipherText = request.Body.CipherText;
        message.CipherNonce = request.Body.CipherNonce;
        message.CipherKeyId = request.Body.CipherKeyId.Trim();
        message.EncryptionAlgorithm = NormalizeAlgorithm(request.Body.EncryptionAlgorithm);
        message.AssociatedDataJson = NormalizeOptionalJson(request.AssociatedDataJson);
        message.ClientSearchTokenHash = NormalizeSearchToken(request.ClientSearchTokenHash);
        message.IsEdited = true;
        message.EditedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        var response = MessagingResponseMapper.ToMessageResponse(message);
        await realtimePublisher.MessageUpdatedAsync(conversationId, response, cancellationToken);
        return MessagingOperationResult<MessageResponse>.Success(response);
    }

    public async Task<MessagingOperationResult<MessageResponse>> RecallMessageAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureMessageOwnerAsync(userId, conversationId, messageId, cancellationToken);
        if (!access.Succeeded)
        {
            return MessagingOperationResult<MessageResponse>.NotFound(access.ErrorCode ?? "message_not_found");
        }

        var message = access.Value!;
        var now = clock.UtcNow;
        if (now > message.RecallUntilUtc)
        {
            return MessagingOperationResult<MessageResponse>.Expired("message_recall_window_expired");
        }

        message.State = MessageDeliveryState.Recalled;
        message.RecalledAtUtc = now;
        message.CipherText = [];
        message.CipherNonce = [];
        message.CipherKeyId = "recalled";
        message.AssociatedDataJson = null;
        message.ClientSearchTokenHash = null;
        foreach (var attachment in message.Attachments)
        {
            attachment.EncryptedFileKey = [];
            attachment.KeyNonce = [];
            attachment.KeyId = "recalled";
            attachment.WaveformJson = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var response = MessagingResponseMapper.ToMessageResponse(message);
        await realtimePublisher.MessageUpdatedAsync(conversationId, response, cancellationToken);
        return MessagingOperationResult<MessageResponse>.Success(response);
    }

    public Task<MessagingOperationResult<MessageResponse>> ReactAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        MessageReactionRequest request,
        CancellationToken cancellationToken = default) =>
        UpsertReactionAsync(userId, conversationId, messageId, request.Emoji, add: true, cancellationToken);

    public Task<MessagingOperationResult<MessageResponse>> RemoveReactionAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        string emoji,
        CancellationToken cancellationToken = default) =>
        UpsertReactionAsync(userId, conversationId, messageId, emoji, add: false, cancellationToken);

    public async Task<MessagingOperationResult> MarkReadAsync(
        Guid userId,
        Guid conversationId,
        MarkConversationReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var participantResult = await LoadParticipantAsync(userId, conversationId, track: true, cancellationToken);
        if (!participantResult.Succeeded)
        {
            return MessagingOperationResult.NotFound(participantResult.ErrorCode ?? "conversation_not_found");
        }

        var participant = participantResult.Value!;
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        var now = clock.UtcNow;
        DateTimeOffset readThrough;
        if (request.LastReadMessageId is null)
        {
            readThrough = now;
        }
        else
        {
            var targetMessage = await dbContext.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(message =>
                    message.Id == request.LastReadMessageId &&
                    message.ConversationId == conversationId,
                    cancellationToken);

            if (targetMessage is null)
            {
                return MessagingOperationResult.NotFound("message_not_found");
            }

            readThrough = targetMessage.CreatedAtUtc;
        }

        participant.LastReadMessageId = request.LastReadMessageId;
        participant.LastReadAtUtc = now;
        participant.MarkedUnreadAtUtc = null;

        if (!settings.ReadReceiptsEnabled)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return MessagingOperationResult.Success();
        }

        var receipts = await dbContext.MessageReceipts
            .Include(static receipt => receipt.Message)
            .Where(receipt =>
                receipt.UserId == userId &&
                receipt.Message != null &&
                receipt.Message.ConversationId == conversationId &&
                receipt.Message.CreatedAtUtc <= readThrough &&
                receipt.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var receipt in receipts)
        {
            receipt.ReadAtUtc = now;
            receipt.DeliveredAtUtc ??= now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (receipts.Count > 0)
        {
            await realtimePublisher.ReadReceiptUpdatedAsync(conversationId, userId, request.LastReadMessageId, now, cancellationToken);
        }

        return MessagingOperationResult.Success();
    }

    public async Task<MessagingOperationResult<ConversationSummaryResponse>> UpdateParticipantAsync(
        Guid userId,
        Guid conversationId,
        UpdateConversationParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await LoadConversationAsync(conversationId, track: true, cancellationToken);
        if (conversation is null)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.NotFound("conversation_not_found");
        }

        var participant = conversation.Participants.FirstOrDefault(item => item.UserId == userId && item.IsActive);
        if (participant is null)
        {
            return MessagingOperationResult<ConversationSummaryResponse>.Forbidden("conversation_access_denied");
        }

        participant.MutedUntilUtc = request.MutedUntilUtc;
        participant.ArchivedAtUtc = request.ArchivedAtUtc;
        participant.PinnedAtUtc = request.PinnedAtUtc;
        participant.MarkedUnreadAtUtc = request.MarkUnread ? clock.UtcNow : null;
        participant.ClearedAtUtc = request.ClearedAtUtc;
        if (request.NotificationSoundEnabled is not null)
        {
            participant.NotificationSoundEnabled = request.NotificationSoundEnabled.Value;
        }

        participant.ThemeKey = NormalizeOptionalKey(request.ThemeKey, maxLength: 64);
        participant.BackgroundKey = NormalizeOptionalKey(request.BackgroundKey, maxLength: 128);

        if (request.Nickname is null)
        {
            participant.NicknameCipherText = null;
            participant.NicknameNonce = null;
            participant.NicknameKeyId = null;
        }
        else
        {
            if (!IsValidEnvelope(request.Nickname))
            {
                return MessagingOperationResult<ConversationSummaryResponse>.ValidationFailed("invalid_nickname_envelope");
            }

            participant.NicknameCipherText = request.Nickname.CipherText;
            participant.NicknameNonce = request.Nickname.CipherNonce;
            participant.NicknameKeyId = request.Nickname.CipherKeyId.Trim();
        }

        conversation.UpdatedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var unread = await CountUnreadMessagesAsync(userId, [participant], cancellationToken);
        var response = MessagingResponseMapper.ToConversationSummary(conversation, participant, unread.GetValueOrDefault(conversationId));
        await realtimePublisher.ConversationUpdatedAsync(conversationId, response, cancellationToken);
        return MessagingOperationResult<ConversationSummaryResponse>.Success(response);
    }

    public async Task<MessagingOperationResult<MessagingSettingsResponse>> GetSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<MessagingSettingsResponse>.Unauthorized("authenticated_user_required");
        }

        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        return MessagingOperationResult<MessagingSettingsResponse>.Success(ToSettingsResponse(settings));
    }

    public async Task<MessagingOperationResult<MessagingSettingsResponse>> UpdateSettingsAsync(
        Guid userId,
        UpdateMessagingSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<MessagingSettingsResponse>.Unauthorized("authenticated_user_required");
        }

        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        settings.MessagePermission = request.MessagePermission;
        settings.GroupInvitePermission = request.GroupInvitePermission;
        settings.OnlineVisibility = request.OnlineVisibility;
        settings.ReadReceiptsEnabled = request.ReadReceiptsEnabled;
        settings.TypingIndicatorsEnabled = request.TypingIndicatorsEnabled;
        settings.LinkPreviewsEnabled = request.LinkPreviewsEnabled;
        settings.ShowMessagePreviewInNotifications = request.ShowMessagePreviewInNotifications;
        settings.UpdatedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MessagingOperationResult<MessagingSettingsResponse>.Success(ToSettingsResponse(settings));
    }

    public async Task<MessagingOperationResult<MessagingDeviceResponse>> RegisterDeviceAsync(
        Guid userId,
        RegisterMessagingDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<MessagingDeviceResponse>.Unauthorized("authenticated_user_required");
        }

        if (string.IsNullOrWhiteSpace(request.DeviceId) ||
            string.IsNullOrWhiteSpace(request.PublicIdentityKey) ||
            string.IsNullOrWhiteSpace(request.SignedPreKey) ||
            string.IsNullOrWhiteSpace(request.SignedPreKeySignature))
        {
            return MessagingOperationResult<MessagingDeviceResponse>.ValidationFailed("invalid_device_key_bundle");
        }

        var normalizedDeviceId = request.DeviceId.Trim();
        var existing = await dbContext.UserDevices.FirstOrDefaultAsync(
            device => device.UserId == userId && device.DeviceId == normalizedDeviceId,
            cancellationToken);
        var now = clock.UtcNow;
        var device = existing ?? new MessagingUserDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = normalizedDeviceId,
            CreatedAtUtc = now
        };

        device.PublicIdentityKey = request.PublicIdentityKey.Trim();
        device.SignedPreKey = request.SignedPreKey.Trim();
        device.SignedPreKeySignature = request.SignedPreKeySignature.Trim();
        device.OneTimePreKeysJson = NormalizeOptionalJson(request.OneTimePreKeysJson);
        device.SupportedAlgorithms = string.IsNullOrWhiteSpace(request.SupportedAlgorithms)
            ? "x25519-ed25519-xchacha20-poly1305"
            : request.SupportedAlgorithms.Trim();
        device.RevokedAtUtc = null;

        if (existing is null)
        {
            dbContext.UserDevices.Add(device);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return MessagingOperationResult<MessagingDeviceResponse>.Success(ToDeviceResponse(device));
    }

    public async Task<MessagingOperationResult> RevokeDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult.Unauthorized("authenticated_user_required");
        }

        var device = await dbContext.UserDevices.FirstOrDefaultAsync(
            item => item.UserId == userId && item.DeviceId == deviceId,
            cancellationToken);
        if (device is null)
        {
            return MessagingOperationResult.NotFound("device_not_found");
        }

        device.RevokedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MessagingOperationResult.Success();
    }

    public async Task<MessagingOperationResult<IReadOnlyCollection<MessagingDeviceKeyBundleResponse>>> ListUserDevicesAsync(
        Guid userId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<IReadOnlyCollection<MessagingDeviceKeyBundleResponse>>.Unauthorized("authenticated_user_required");
        }

        if (targetUserId == Guid.Empty)
        {
            return MessagingOperationResult<IReadOnlyCollection<MessagingDeviceKeyBundleResponse>>.ValidationFailed("invalid_target_user");
        }

        var devices = await dbContext.UserDevices
            .AsNoTracking()
            .Where(device => device.UserId == targetUserId && device.RevokedAtUtc == null)
            .OrderByDescending(static device => device.CreatedAtUtc)
            .Take(20)
            .Select(static device => new MessagingDeviceKeyBundleResponse(
                device.UserId,
                device.DeviceId,
                device.PublicIdentityKey,
                device.SignedPreKey,
                device.SignedPreKeySignature,
                device.OneTimePreKeysJson,
                device.SupportedAlgorithms,
                device.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return MessagingOperationResult<IReadOnlyCollection<MessagingDeviceKeyBundleResponse>>.Success(devices);
    }

    public async Task<MessagingOperationResult<IReadOnlyCollection<MessageAttachmentResponse>>> ListConversationMediaAsync(
        Guid userId,
        Guid conversationId,
        MessageAttachmentKind kind,
        int pageSize,
        DateTimeOffset? beforeUtc,
        CancellationToken cancellationToken = default)
    {
        var participantResult = await LoadParticipantAsync(userId, conversationId, track: false, cancellationToken);
        if (!participantResult.Succeeded)
        {
            return MessagingOperationResult<IReadOnlyCollection<MessageAttachmentResponse>>.NotFound("conversation_not_found");
        }

        var participant = participantResult.Value!;
        var now = clock.UtcNow;
        var safePageSize = SafePageSize(pageSize);
        var query = dbContext.MessageAttachments
            .AsNoTracking()
            .Include(static attachment => attachment.Message)
            .Where(attachment =>
                attachment.Kind == kind &&
                attachment.Message != null &&
                attachment.Message.ConversationId == conversationId &&
                (attachment.Message.ExpiresAtUtc == null || attachment.Message.ExpiresAtUtc > now) &&
                (participant.ClearedAtUtc == null || attachment.Message.CreatedAtUtc > participant.ClearedAtUtc));

        if (beforeUtc is not null)
        {
            query = query.Where(attachment => attachment.CreatedAtUtc < beforeUtc.Value);
        }

        var items = await query
            .OrderByDescending(static attachment => attachment.CreatedAtUtc)
            .Take(safePageSize)
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
            .ToArrayAsync(cancellationToken);

        return MessagingOperationResult<IReadOnlyCollection<MessageAttachmentResponse>>.Success(items);
    }

    public async Task<MessagingOperationResult<MessagePageResponse>> SearchMessagesAsync(
        Guid userId,
        SearchConversationMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        var participantResult = await LoadParticipantAsync(userId, request.ConversationId, track: false, cancellationToken);
        if (!participantResult.Succeeded)
        {
            return MessagingOperationResult<MessagePageResponse>.NotFound("conversation_not_found");
        }

        if (request.ClientSearchTokenHashes.Count == 0 && request.AttachmentKind is null && request.SenderUserId is null && request.FromUtc is null && request.ToUtc is null)
        {
            return MessagingOperationResult<MessagePageResponse>.ValidationFailed("search_filter_required");
        }

        var participant = participantResult.Value!;
        var now = clock.UtcNow;
        var safePageSize = SafePageSize(request.PageSize);
        var tokenHashes = request.ClientSearchTokenHashes
            .Where(static token => !string.IsNullOrWhiteSpace(token))
            .Select(static token => NormalizeSearchToken(token))
            .Where(static token => token is not null)
            .Select(static token => token!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var query = dbContext.Messages
            .AsNoTracking()
            .Include(static message => message.Attachments)
            .Include(static message => message.Reactions)
            .Include(static message => message.Receipts)
            .Where(message =>
                message.ConversationId == request.ConversationId &&
                (message.ExpiresAtUtc == null || message.ExpiresAtUtc > now) &&
                (participant.ClearedAtUtc == null || message.CreatedAtUtc > participant.ClearedAtUtc));

        if (tokenHashes.Length > 0)
        {
            var token = tokenHashes[0];
            query = query.Where(message => message.ClientSearchTokenHash != null && message.ClientSearchTokenHash.Contains(token));
        }

        if (request.SenderUserId is not null)
        {
            query = query.Where(message => message.SenderUserId == request.SenderUserId.Value);
        }

        if (request.FromUtc is not null)
        {
            query = query.Where(message => message.CreatedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc is not null)
        {
            query = query.Where(message => message.CreatedAtUtc <= request.ToUtc.Value);
        }

        if (request.AttachmentKind is not null)
        {
            query = query.Where(message => message.Attachments.Any(attachment => attachment.Kind == request.AttachmentKind.Value));
        }

        if (request.BeforeUtc is not null)
        {
            query = query.Where(message => message.CreatedAtUtc < request.BeforeUtc.Value);
        }

        var messages = await query
            .OrderByDescending(static message => message.CreatedAtUtc)
            .Take(safePageSize + 1)
            .ToListAsync(cancellationToken);

        var pageItems = messages.Take(safePageSize).ToArray();
        var replyPreviews = await LoadReplyPreviewsAsync(pageItems, cancellationToken);
        var response = pageItems
            .Select(message => MessagingResponseMapper.ToMessageResponse(message, replyPreviews.GetValueOrDefault(message.ReplyToMessageId ?? Guid.Empty)))
            .ToArray();
        return MessagingOperationResult<MessagePageResponse>.Success(
            new MessagePageResponse(response, safePageSize, messages.Count > safePageSize ? response.LastOrDefault()?.CreatedAtUtc : null));
    }

    public async Task<MessagingOperationResult<MessagingReportResponse>> ReportAsync(
        Guid userId,
        Guid conversationId,
        ReportConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var participantResult = await LoadParticipantAsync(userId, conversationId, track: false, cancellationToken);
        if (!participantResult.Succeeded)
        {
            return MessagingOperationResult<MessagingReportResponse>.NotFound("conversation_not_found");
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return MessagingOperationResult<MessagingReportResponse>.ValidationFailed("report_reason_required");
        }

        if (request.MessageId is not null)
        {
            var messageExists = await dbContext.Messages.AnyAsync(
                message => message.Id == request.MessageId && message.ConversationId == conversationId,
                cancellationToken);
            if (!messageExists)
            {
                return MessagingOperationResult<MessagingReportResponse>.NotFound("message_not_found");
            }
        }

        var report = new MessagingConversationReport
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            ReporterUserId = userId,
            ReportedUserId = request.ReportedUserId,
            MessageId = request.MessageId,
            ReasonCode = request.ReasonCode.Trim(),
            MetadataJson = NormalizeOptionalJson(request.MetadataJson),
            UserProvidedEvidenceCipherText = request.UserProvidedEvidence?.CipherText,
            UserProvidedEvidenceNonce = request.UserProvidedEvidence?.CipherNonce,
            EvidenceKeyId = request.UserProvidedEvidence?.CipherKeyId,
            CreatedAtUtc = clock.UtcNow
        };

        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MessagingOperationResult<MessagingReportResponse>.Success(
            new MessagingReportResponse(report.Id, report.Status, report.CreatedAtUtc));
    }

    public async Task<MessagingOperationResult> AcceptRequestAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await LoadConversationAsync(conversationId, track: true, cancellationToken);
        if (conversation is null)
        {
            return MessagingOperationResult.NotFound("conversation_not_found");
        }

        if (!conversation.Participants.Any(participant => participant.UserId == userId && participant.IsActive))
        {
            return MessagingOperationResult.Forbidden("conversation_access_denied");
        }

        conversation.RequestStatus = ConversationRequestStatus.Accepted;
        conversation.UpdatedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MessagingOperationResult.Success();
    }

    public async Task<MessagingOperationResult> RejectRequestAsync(
        Guid userId,
        Guid conversationId,
        bool blockSender,
        CancellationToken cancellationToken = default)
    {
        var conversation = await LoadConversationAsync(conversationId, track: true, cancellationToken);
        if (conversation is null)
        {
            return MessagingOperationResult.NotFound("conversation_not_found");
        }

        var participant = conversation.Participants.FirstOrDefault(item => item.UserId == userId && item.IsActive);
        if (participant is null)
        {
            return MessagingOperationResult.Forbidden("conversation_access_denied");
        }

        conversation.RequestStatus = blockSender ? ConversationRequestStatus.Blocked : ConversationRequestStatus.Deleted;
        participant.DeletedAtUtc = clock.UtcNow;
        conversation.UpdatedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MessagingOperationResult.Success();
    }

    public async Task<MessagingOperationResult<bool>> CanAccessConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var participant = await dbContext.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(item => item.UserId == userId && item.ConversationId == conversationId && item.DeletedAtUtc == null && item.LeftAtUtc == null, cancellationToken);

        return MessagingOperationResult<bool>.Success(participant);
    }

    private async Task<MessagingOperationResult<MessageResponse>> UpsertReactionAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        string emoji,
        bool add,
        CancellationToken cancellationToken)
    {
        var access = await LoadParticipantAsync(userId, conversationId, track: false, cancellationToken);
        if (!access.Succeeded)
        {
            return MessagingOperationResult<MessageResponse>.NotFound("conversation_not_found");
        }

        var normalizedEmoji = emoji.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmoji) || normalizedEmoji.Length > 32)
        {
            return MessagingOperationResult<MessageResponse>.ValidationFailed("invalid_emoji");
        }

        var message = await dbContext.Messages
            .Include(static item => item.Attachments)
            .Include(static item => item.Reactions)
            .Include(static item => item.Receipts)
            .FirstOrDefaultAsync(item => item.Id == messageId && item.ConversationId == conversationId, cancellationToken);

        if (message is null)
        {
            return MessagingOperationResult<MessageResponse>.NotFound("message_not_found");
        }

        var existing = message.Reactions.FirstOrDefault(reaction => reaction.UserId == userId && reaction.Emoji == normalizedEmoji);
        if (add && existing is null)
        {
            message.Reactions.Add(new MessagingMessageReaction
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                UserId = userId,
                Emoji = normalizedEmoji,
                CreatedAtUtc = clock.UtcNow
            });
        }
        else if (!add && existing is not null)
        {
            dbContext.MessageReactions.Remove(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var response = MessagingResponseMapper.ToMessageResponse(message);
        await realtimePublisher.MessageUpdatedAsync(conversationId, response, cancellationToken);
        return MessagingOperationResult<MessageResponse>.Success(response);
    }

    private async Task<MessagingOperationResult<MessagingMessage>> EnsureMessageOwnerAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<MessagingMessage>.Unauthorized("authenticated_user_required");
        }

        var hasAccess = await dbContext.ConversationParticipants.AnyAsync(
            participant =>
                participant.UserId == userId &&
                participant.ConversationId == conversationId &&
                participant.DeletedAtUtc == null &&
                participant.LeftAtUtc == null,
            cancellationToken);

        if (!hasAccess)
        {
            return MessagingOperationResult<MessagingMessage>.Forbidden("conversation_access_denied");
        }

        var message = await dbContext.Messages
            .Include(static item => item.Attachments)
            .Include(static item => item.Reactions)
            .Include(static item => item.Receipts)
            .FirstOrDefaultAsync(item => item.Id == messageId && item.ConversationId == conversationId, cancellationToken);

        if (message is null)
        {
            return MessagingOperationResult<MessagingMessage>.NotFound("message_not_found");
        }

        if (message.SenderUserId != userId)
        {
            return MessagingOperationResult<MessagingMessage>.Forbidden("message_owner_required");
        }

        return MessagingOperationResult<MessagingMessage>.Success(message);
    }

    private async Task<Dictionary<Guid, int>> CountUnreadMessagesAsync(
        Guid userId,
        IReadOnlyCollection<MessagingConversationParticipant> participants,
        CancellationToken cancellationToken)
    {
        if (participants.Count == 0)
        {
            return [];
        }

        var watermarks = participants
            .GroupBy(static participant => participant.ConversationId)
            .ToDictionary(
                static group => group.Key,
                static group => group.Max(participant => participant.LastReadAtUtc ?? participant.ClearedAtUtc ?? DateTimeOffset.MinValue));
        var conversationIds = watermarks.Keys.ToArray();
        var minimumWatermark = watermarks.Values.Min();
        var messages = await dbContext.Messages
            .AsNoTracking()
            .Where(message =>
                conversationIds.Contains(message.ConversationId) &&
                message.SenderUserId != userId &&
                message.State != MessageDeliveryState.Recalled &&
                message.CreatedAtUtc > minimumWatermark &&
                (message.ExpiresAtUtc == null || message.ExpiresAtUtc > clock.UtcNow))
            .Select(static message => new { message.ConversationId, message.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        return messages
            .Where(message => message.CreatedAtUtc > watermarks[message.ConversationId])
            .GroupBy(static message => message.ConversationId)
            .ToDictionary(static group => group.Key, static group => group.Count());
    }

    private static IReadOnlyCollection<MessagingConversationParticipant> DeduplicateConversationParticipants(
        IReadOnlyCollection<MessagingConversationParticipant> participants)
    {
        if (participants.Count < 2)
        {
            return participants;
        }

        var seenConversationIds = new HashSet<Guid>();
        var deduplicated = new List<MessagingConversationParticipant>(participants.Count);
        foreach (var participant in participants)
        {
            if (!seenConversationIds.Add(participant.ConversationId))
            {
                continue;
            }

            deduplicated.Add(participant);
        }

        return deduplicated;
    }

    private async Task<Dictionary<Guid, MessagingMessage>> LoadReplyPreviewsAsync(
        IReadOnlyCollection<MessagingMessage> messages,
        CancellationToken cancellationToken)
    {
        var replyIds = messages
            .Select(static message => message.ReplyToMessageId)
            .Where(static id => id is not null)
            .Select(static id => id!.Value)
            .Distinct()
            .ToArray();

        if (replyIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Messages
            .AsNoTracking()
            .Include(static message => message.Attachments)
            .Include(static message => message.Reactions)
            .Include(static message => message.Receipts)
            .Where(message => replyIds.Contains(message.Id))
            .ToDictionaryAsync(static message => message.Id, cancellationToken);
    }

    private async Task<MessagingConversation?> LoadConversationAsync(Guid conversationId, bool track, CancellationToken cancellationToken)
    {
        var query = dbContext.Conversations
            .Include(static conversation => conversation.Participants)
            .Where(conversation => conversation.Id == conversationId && conversation.Status != ConversationStatus.Deleted);

        return track
            ? await query.FirstOrDefaultAsync(cancellationToken)
            : await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<MessagingOperationResult<MessagingConversationParticipant>> LoadParticipantAsync(
        Guid userId,
        Guid conversationId,
        bool track,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return MessagingOperationResult<MessagingConversationParticipant>.Unauthorized("authenticated_user_required");
        }

        var query = dbContext.ConversationParticipants
            .Where(participant =>
                participant.UserId == userId &&
                participant.ConversationId == conversationId &&
                participant.DeletedAtUtc == null &&
                participant.LeftAtUtc == null);

        var participant = track
            ? await query.FirstOrDefaultAsync(cancellationToken)
            : await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        return participant is null
            ? MessagingOperationResult<MessagingConversationParticipant>.NotFound("conversation_not_found")
            : MessagingOperationResult<MessagingConversationParticipant>.Success(participant);
    }

    private async Task<MessagingOperationResult> ResolveDirectPrivacyAsync(
        Guid senderUserId,
        Guid recipientUserId,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(recipientUserId, cancellationToken);
        var relationship = await relationshipReader.GetAsync(senderUserId, recipientUserId, cancellationToken);
        if (relationship.IsBlockedByRequester || relationship.IsBlockedByTarget || settings.MessagePermission == MessagingPermission.Nobody)
        {
            return MessagingOperationResult.Forbidden("messaging_not_allowed");
        }

        return settings.MessagePermission switch
        {
            MessagingPermission.Everyone => MessagingOperationResult.Success(),
            MessagingPermission.Followers when relationship.IsFollowedBy => MessagingOperationResult.Success(),
            MessagingPermission.Following when relationship.IsFollowing => MessagingOperationResult.Success(),
            MessagingPermission.Mutuals when relationship.IsMutual => MessagingOperationResult.Success(),
            _ => MessagingOperationResult.Conflict("message_request_required")
        };
    }

    private async Task<MessagingOperationResult> ResolveGroupInvitePrivacyAsync(
        Guid inviterUserId,
        Guid inviteeUserId,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(inviteeUserId, cancellationToken);
        var relationship = await relationshipReader.GetAsync(inviterUserId, inviteeUserId, cancellationToken);
        if (relationship.IsBlockedByRequester || relationship.IsBlockedByTarget || settings.GroupInvitePermission == MessagingGroupInvitePermission.Nobody)
        {
            return MessagingOperationResult.Forbidden("group_invite_not_allowed");
        }

        return settings.GroupInvitePermission switch
        {
            MessagingGroupInvitePermission.Everyone => MessagingOperationResult.Success(),
            MessagingGroupInvitePermission.Following when relationship.IsFollowing => MessagingOperationResult.Success(),
            MessagingGroupInvitePermission.Mutuals when relationship.IsMutual => MessagingOperationResult.Success(),
            _ => MessagingOperationResult.Forbidden("group_invite_not_allowed")
        };
    }

    private async Task<MessagingUserSettings> GetOrCreateSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await dbContext.UserSettings.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new MessagingUserSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UpdatedAtUtc = clock.UtcNow
        };
        dbContext.UserSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private MessagingConversationParticipant CreateParticipant(
        Guid conversationId,
        Guid userId,
        ConversationParticipantRole role,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = userId,
            Role = role,
            JoinedAtUtc = now
        };

    private string? ValidateEncryptedMessage(SendEncryptedMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientMessageId) || request.ClientMessageId.Length > 128)
        {
            return "invalid_client_message_id";
        }

        if (string.IsNullOrWhiteSpace(request.SenderDeviceId) || request.SenderDeviceId.Length > 128)
        {
            return "invalid_sender_device_id";
        }

        if (!IsValidEnvelope(request.Body))
        {
            return "invalid_encrypted_body";
        }

        if (request.Body.CipherText.Length > _rules.MaxMessageCipherTextBytes)
        {
            return "message_ciphertext_too_large";
        }

        if (request.Attachments.Count > _rules.MaxAttachmentCount)
        {
            return "attachment_limit_exceeded";
        }

        foreach (var attachment in request.Attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.StorageKey) ||
                string.IsNullOrWhiteSpace(attachment.ContentType) ||
                string.IsNullOrWhiteSpace(attachment.KeyId) ||
                attachment.EncryptedFileKey.Length == 0 ||
                attachment.KeyNonce.Length == 0 ||
                attachment.SizeBytes < 0)
            {
                return "invalid_attachment_envelope";
            }
        }

        return null;
    }

    private static bool IsValidEnvelope(EncryptedTextEnvelope envelope) =>
        envelope.CipherText.Length > 0 &&
        envelope.CipherNonce.Length > 0 &&
        !string.IsNullOrWhiteSpace(envelope.CipherKeyId) &&
        !string.IsNullOrWhiteSpace(envelope.EncryptionAlgorithm);

    private int SafePageSize(int pageSize) => Math.Clamp(pageSize, 1, _rules.MaxPageSize);

    private static string NormalizeAlgorithm(string algorithm) =>
        string.IsNullOrWhiteSpace(algorithm)
            ? "signal-x3dh-double-ratchet/xchacha20-poly1305"
            : algorithm.Trim().ToLowerInvariant();

    private static string? NormalizeOptionalJson(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeSearchToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder();
        var pendingSpace = false;
        foreach (var character in value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
            if (builder.Length >= 128)
            {
                break;
            }
        }

        var normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeOptionalKey(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static MessagingSettingsResponse ToSettingsResponse(MessagingUserSettings settings) =>
        new(
            settings.MessagePermission,
            settings.GroupInvitePermission,
            settings.OnlineVisibility,
            settings.ReadReceiptsEnabled,
            settings.TypingIndicatorsEnabled,
            settings.LinkPreviewsEnabled,
            settings.ShowMessagePreviewInNotifications,
            settings.UpdatedAtUtc);

    private static MessagingDeviceResponse ToDeviceResponse(MessagingUserDevice device) =>
        new(
            device.Id,
            device.DeviceId,
            device.PublicIdentityKey,
            device.SignedPreKey,
            device.SignedPreKeySignature,
            device.OneTimePreKeysJson,
            device.SupportedAlgorithms,
            device.CreatedAtUtc,
            device.RevokedAtUtc);
}
