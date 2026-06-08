// <copyright file="IMessagingService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Application.Models;
using Norge360.MessagingService.Contracts.Requests;
using Norge360.MessagingService.Contracts.Responses;
using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Application.Abstractions;

public interface IMessagingService
{
    Task<MessagingOperationResult<ConversationPageResponse>> ListConversationsAsync(Guid userId, int pageSize, DateTimeOffset? beforeUtc, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessagePageResponse>> ListMessagesAsync(Guid userId, Guid conversationId, int pageSize, DateTimeOffset? beforeUtc, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<ConversationSummaryResponse>> CreateDirectConversationAsync(Guid userId, CreateDirectConversationRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<ConversationSummaryResponse>> CreateGroupConversationAsync(Guid userId, CreateGroupConversationRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessageResponse>> SendMessageAsync(Guid userId, Guid conversationId, SendEncryptedMessageRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessageResponse>> EditMessageAsync(Guid userId, Guid conversationId, Guid messageId, EditEncryptedMessageRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessageResponse>> RecallMessageAsync(Guid userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessageResponse>> ReactAsync(Guid userId, Guid conversationId, Guid messageId, MessageReactionRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessageResponse>> RemoveReactionAsync(Guid userId, Guid conversationId, Guid messageId, string emoji, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult> MarkReadAsync(Guid userId, Guid conversationId, MarkConversationReadRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<ConversationSummaryResponse>> UpdateParticipantAsync(Guid userId, Guid conversationId, UpdateConversationParticipantRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessagingSettingsResponse>> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessagingSettingsResponse>> UpdateSettingsAsync(Guid userId, UpdateMessagingSettingsRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessagingDeviceResponse>> RegisterDeviceAsync(Guid userId, RegisterMessagingDeviceRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult> RevokeDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<IReadOnlyCollection<MessagingDeviceKeyBundleResponse>>> ListUserDevicesAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<IReadOnlyCollection<MessageAttachmentResponse>>> ListConversationMediaAsync(Guid userId, Guid conversationId, MessageAttachmentKind kind, int pageSize, DateTimeOffset? beforeUtc, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessagePageResponse>> SearchMessagesAsync(Guid userId, SearchConversationMessagesRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<MessagingReportResponse>> ReportAsync(Guid userId, Guid conversationId, ReportConversationRequest request, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult> AcceptRequestAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult> RejectRequestAsync(Guid userId, Guid conversationId, bool blockSender, CancellationToken cancellationToken = default);
    Task<MessagingOperationResult<bool>> CanAccessConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
}
