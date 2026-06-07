// <copyright file="NotificationsController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.CurrentUser;
using Norge360.Notification.Application.Abstractions;
using Norge360.Notification.Contracts.Notifications.Requests;
using Norge360.Notification.Contracts.Notifications.Responses;

namespace Norge360.Notification.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(
    IInAppNotificationService inAppNotificationService,
    IUserNotificationPreferenceService preferenceService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<InAppNotificationsPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool markAsSeen = true,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrEmpty();
        if (userId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        var response = await inAppNotificationService.ListAsync(userId, page, pageSize, markAsSeen, cancellationToken);
        return Ok(response);
    }

    [HttpGet("summary")]
    [ProducesResponseType<NotificationSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrEmpty();
        if (userId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        return Ok(await inAppNotificationService.GetSummaryAsync(userId, cancellationToken));
    }

    [HttpPost("seen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkSeen(CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrEmpty();
        if (userId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        await inAppNotificationService.MarkAllAsSeenAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("preferences")]
    [ProducesResponseType<NotificationPreferencesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Preferences(CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrEmpty();
        if (userId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        return Ok(await preferenceService.GetAsync(userId, cancellationToken));
    }

    [HttpPut("preferences")]
    [ProducesResponseType<NotificationPreferencesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrEmpty();
        if (userId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        return Ok(await preferenceService.UpdateAsync(userId, request, cancellationToken));
    }

    private Guid GetUserIdOrEmpty() =>
        currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty
            ? currentUserService.UserId
            : Guid.Empty;

    private ObjectResult UnauthorizedProblem() => Problem(
        title: "Unauthorized",
        detail: "Authentication is required to access notifications.",
        statusCode: StatusCodes.Status401Unauthorized);
}
