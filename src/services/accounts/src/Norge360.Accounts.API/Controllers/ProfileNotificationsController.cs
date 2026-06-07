// <copyright file="ProfileNotificationsController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Responses;
using Norge360.CurrentUser;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts/profile-notifications")]
public sealed class ProfileNotificationsController(
    IProfileNotificationSubscriptionService subscriptionService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("{username}")]
    [ProducesResponseType<ProfileNotificationSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.GetByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("{username}")]
    [ProducesResponseType<ProfileNotificationSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubscribeByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.SubscribeByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return MapResult(result);
    }

    [HttpDelete("{username}")]
    [ProducesResponseType<ProfileNotificationSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnsubscribeByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.UnsubscribeByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return MapResult(result);
    }

    private IActionResult MapResult(ProfileNotificationSubscriptionResult result) =>
        result.Status switch
        {
            ProfileNotificationSubscriptionStatus.Success => Ok(new ProfileNotificationSubscriptionResponse(result.IsSubscribed)),
            ProfileNotificationSubscriptionStatus.ValidationFailed => ValidationProblem(result.ErrorCode),
            ProfileNotificationSubscriptionStatus.NotFound => ProfileNotFound(),
            ProfileNotificationSubscriptionStatus.ProvisioningPending => ProfileProvisioningPending(),
            _ => UnauthorizedProblem()
        };

    private Guid GetUserIdOrEmpty() =>
        currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty
            ? currentUserService.UserId
            : Guid.Empty;

    private ObjectResult ValidationProblem(string? errorCode)
    {
        var details = new ProblemDetails
        {
            Title = "Validation failed",
            Detail = "The profile notification request is invalid.",
            Status = StatusCodes.Status400BadRequest
        };

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            details.Extensions["errorCode"] = errorCode;
        }

        return StatusCode(StatusCodes.Status400BadRequest, details);
    }

    private ObjectResult ProfileProvisioningPending() => Problem(
        title: "Profile provisioning pending",
        detail: "Your profile has not been provisioned yet. Please try again shortly.",
        statusCode: StatusCodes.Status202Accepted);

    private ObjectResult ProfileNotFound() => Problem(
        title: "Profile not found",
        detail: "The requested profile was not found.",
        statusCode: StatusCodes.Status404NotFound);

    private ObjectResult UnauthorizedProblem() => Problem(
        title: "Unauthorized",
        detail: "Authentication is required to manage profile notifications.",
        statusCode: StatusCodes.Status401Unauthorized);
}
