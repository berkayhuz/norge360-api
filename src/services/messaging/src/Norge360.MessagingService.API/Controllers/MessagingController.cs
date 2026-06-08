// <copyright file="MessagingController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.CurrentUser;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Application.Models;
using Norge360.MessagingService.Contracts.Requests;
using Norge360.MessagingService.Contracts.Responses;
using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/messaging")]
public sealed class MessagingController(
    IMessagingService messagingService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { service = "messaging", status = "ok" });

    [HttpGet("conversations")]
    [ProducesResponseType<ConversationPageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConversations(
        [FromQuery] int pageSize = 30,
        [FromQuery] DateTimeOffset? beforeUtc = null,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await messagingService.ListConversationsAsync(GetUserIdOrEmpty(), pageSize, beforeUtc, cancellationToken));

    [HttpPost("conversations/direct")]
    [ProducesResponseType<ConversationSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDirect(
        [FromBody] CreateDirectConversationRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.CreateDirectConversationAsync(GetUserIdOrEmpty(), request, cancellationToken));

    [HttpPost("conversations/groups")]
    [ProducesResponseType<ConversationSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupConversationRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.CreateGroupConversationAsync(GetUserIdOrEmpty(), request, cancellationToken));

    [HttpGet("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType<MessagePageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMessages(
        Guid conversationId,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTimeOffset? beforeUtc = null,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await messagingService.ListMessagesAsync(GetUserIdOrEmpty(), conversationId, pageSize, beforeUtc, cancellationToken));

    [HttpPost("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendEncryptedMessageRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.SendMessageAsync(GetUserIdOrEmpty(), conversationId, request, cancellationToken));

    [HttpPatch("conversations/{conversationId:guid}/messages/{messageId:guid}")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EditMessage(
        Guid conversationId,
        Guid messageId,
        [FromBody] EditEncryptedMessageRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.EditMessageAsync(GetUserIdOrEmpty(), conversationId, messageId, request, cancellationToken));

    [HttpPost("conversations/{conversationId:guid}/messages/{messageId:guid}/recall")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecallMessage(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.RecallMessageAsync(GetUserIdOrEmpty(), conversationId, messageId, cancellationToken));

    [HttpPost("conversations/{conversationId:guid}/messages/{messageId:guid}/reactions")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> React(
        Guid conversationId,
        Guid messageId,
        [FromBody] MessageReactionRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.ReactAsync(GetUserIdOrEmpty(), conversationId, messageId, request, cancellationToken));

    [HttpDelete("conversations/{conversationId:guid}/messages/{messageId:guid}/reactions/{emoji}")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveReaction(
        Guid conversationId,
        Guid messageId,
        string emoji,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.RemoveReactionAsync(GetUserIdOrEmpty(), conversationId, messageId, Uri.UnescapeDataString(emoji), cancellationToken));

    [HttpPost("conversations/{conversationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(
        Guid conversationId,
        [FromBody] MarkConversationReadRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.MarkReadAsync(GetUserIdOrEmpty(), conversationId, request, cancellationToken));

    [HttpPatch("conversations/{conversationId:guid}/me")]
    [ProducesResponseType<ConversationSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateParticipant(
        Guid conversationId,
        [FromBody] UpdateConversationParticipantRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.UpdateParticipantAsync(GetUserIdOrEmpty(), conversationId, request, cancellationToken));

    [HttpGet("conversations/{conversationId:guid}/media")]
    [ProducesResponseType<IReadOnlyCollection<MessageAttachmentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMedia(
        Guid conversationId,
        [FromQuery] MessageAttachmentKind kind,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTimeOffset? beforeUtc = null,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await messagingService.ListConversationMediaAsync(GetUserIdOrEmpty(), conversationId, kind, pageSize, beforeUtc, cancellationToken));

    [HttpPost("search")]
    [ProducesResponseType<MessagePageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromBody] SearchConversationMessagesRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.SearchMessagesAsync(GetUserIdOrEmpty(), request, cancellationToken));

    [HttpGet("settings")]
    [ProducesResponseType<MessagingSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.GetSettingsAsync(GetUserIdOrEmpty(), cancellationToken));

    [HttpPut("settings")]
    [ProducesResponseType<MessagingSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateMessagingSettingsRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.UpdateSettingsAsync(GetUserIdOrEmpty(), request, cancellationToken));

    [HttpPost("devices")]
    [ProducesResponseType<MessagingDeviceResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterMessagingDeviceRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.RegisterDeviceAsync(GetUserIdOrEmpty(), request, cancellationToken));

    [HttpDelete("devices/{deviceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeDevice(string deviceId, CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.RevokeDeviceAsync(GetUserIdOrEmpty(), deviceId, cancellationToken));

    [HttpGet("users/{targetUserId:guid}/devices")]
    [ProducesResponseType<IReadOnlyCollection<MessagingDeviceKeyBundleResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUserDevices(Guid targetUserId, CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.ListUserDevicesAsync(GetUserIdOrEmpty(), targetUserId, cancellationToken));

    [HttpPost("conversations/{conversationId:guid}/reports")]
    [ProducesResponseType<MessagingReportResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Report(
        Guid conversationId,
        [FromBody] ReportConversationRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.ReportAsync(GetUserIdOrEmpty(), conversationId, request, cancellationToken));

    [HttpPost("requests/{conversationId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptRequest(Guid conversationId, CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.AcceptRequestAsync(GetUserIdOrEmpty(), conversationId, cancellationToken));

    [HttpPost("requests/{conversationId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectRequest(
        Guid conversationId,
        [FromBody] RejectMessageRequestRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await messagingService.RejectRequestAsync(GetUserIdOrEmpty(), conversationId, request.BlockSender, cancellationToken));

    private Guid GetUserIdOrEmpty() =>
        currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty
            ? currentUserService.UserId
            : Guid.Empty;

    private IActionResult ToActionResult<T>(MessagingOperationResult<T> result) =>
        result.Status switch
        {
            MessagingOperationStatus.Success => Ok(result.Value),
            MessagingOperationStatus.Unauthorized => ProblemResult(StatusCodes.Status401Unauthorized, "Unauthorized", result.ErrorCode ?? "authenticated_user_required"),
            MessagingOperationStatus.Forbidden => ProblemResult(StatusCodes.Status403Forbidden, "Forbidden", result.ErrorCode ?? "forbidden"),
            MessagingOperationStatus.NotFound => ProblemResult(StatusCodes.Status404NotFound, "Not found", result.ErrorCode ?? "not_found"),
            MessagingOperationStatus.ValidationFailed => ProblemResult(StatusCodes.Status400BadRequest, "Validation failed", result.ErrorCode ?? "validation_failed"),
            MessagingOperationStatus.Conflict => ProblemResult(StatusCodes.Status409Conflict, "Conflict", result.ErrorCode ?? "conflict"),
            MessagingOperationStatus.Expired => ProblemResult(StatusCodes.Status422UnprocessableEntity, "Operation window expired", result.ErrorCode ?? "operation_window_expired"),
            _ => ProblemResult(StatusCodes.Status500InternalServerError, "Messaging error", "messaging_error")
        };

    private IActionResult ToActionResult(MessagingOperationResult result) =>
        result.Status switch
        {
            MessagingOperationStatus.Success => NoContent(),
            MessagingOperationStatus.Unauthorized => ProblemResult(StatusCodes.Status401Unauthorized, "Unauthorized", result.ErrorCode ?? "authenticated_user_required"),
            MessagingOperationStatus.Forbidden => ProblemResult(StatusCodes.Status403Forbidden, "Forbidden", result.ErrorCode ?? "forbidden"),
            MessagingOperationStatus.NotFound => ProblemResult(StatusCodes.Status404NotFound, "Not found", result.ErrorCode ?? "not_found"),
            MessagingOperationStatus.ValidationFailed => ProblemResult(StatusCodes.Status400BadRequest, "Validation failed", result.ErrorCode ?? "validation_failed"),
            MessagingOperationStatus.Conflict => ProblemResult(StatusCodes.Status409Conflict, "Conflict", result.ErrorCode ?? "conflict"),
            MessagingOperationStatus.Expired => ProblemResult(StatusCodes.Status422UnprocessableEntity, "Operation window expired", result.ErrorCode ?? "operation_window_expired"),
            _ => ProblemResult(StatusCodes.Status500InternalServerError, "Messaging error", "messaging_error")
        };

    private ObjectResult ProblemResult(int statusCode, string title, string errorCode)
    {
        var details = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = errorCode
        };
        details.Extensions["errorCode"] = errorCode;
        return StatusCode(statusCode, details);
    }
}
