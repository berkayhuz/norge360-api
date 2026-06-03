// <copyright file="FollowsController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.CurrentUser;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Route("api/accounts/follows")]
public sealed class FollowsController(
    IUserFollowService userFollowService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("{username}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await userFollowService.FollowByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return MapMutationResult(result);
    }

    [HttpDelete("{username}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfollowByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await userFollowService.UnfollowByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return MapMutationResult(result);
    }

    private IActionResult MapMutationResult(UserFollowMutationResult result) =>
        result.Status switch
        {
            UserFollowMutationStatus.Success => NoContent(),
            UserFollowMutationStatus.ValidationFailed => ValidationProblem(result.ErrorCode),
            UserFollowMutationStatus.NotFound => ProfileNotFound(),
            UserFollowMutationStatus.ProvisioningPending => ProfileProvisioningPending(),
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
            Detail = "The follow request is invalid.",
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
        detail: "Authentication is required to manage follows.",
        statusCode: StatusCodes.Status401Unauthorized);
}
