// <copyright file="BlocksController.cs" company="Norge360">
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
[Route("api/accounts/blocks")]
public sealed class BlocksController(
    IUserBlockService userBlockService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("{username}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await userBlockService.BlockByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return result.Status switch
        {
            UserBlockMutationStatus.Success => NoContent(),
            UserBlockMutationStatus.ValidationFailed => ValidationProblem(result.ErrorCode),
            UserBlockMutationStatus.NotFound => ProfileNotFound(),
            UserBlockMutationStatus.ProvisioningPending => ProfileProvisioningPending(),
            _ => UnauthorizedProblem()
        };
    }

    [HttpDelete("{username}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockByUsername(string username, CancellationToken cancellationToken)
    {
        var result = await userBlockService.UnblockByUsernameAsync(GetUserIdOrEmpty(), username, cancellationToken);
        return result.Status switch
        {
            UserBlockMutationStatus.Success => NoContent(),
            UserBlockMutationStatus.ValidationFailed => ValidationProblem(result.ErrorCode),
            UserBlockMutationStatus.NotFound => ProfileNotFound(),
            UserBlockMutationStatus.ProvisioningPending => ProfileProvisioningPending(),
            _ => UnauthorizedProblem()
        };
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<BlockedUsersPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMyBlocked(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await userBlockService.ListBlockedAsync(GetUserIdOrEmpty(), page, pageSize, cancellationToken);
        return result.Status switch
        {
            UserBlockListStatus.Success => Ok(new BlockedUsersPageResponse(
                result.Page,
                result.PageSize,
                result.Items.Select(static item => new BlockedUserResponse(
                    item.BlockedProfileId,
                    item.Username,
                    item.DisplayName,
                    item.AvatarUrl,
                    item.BlockedAtUtc)).ToArray())),
            UserBlockListStatus.ProvisioningPending => ProfileProvisioningPending(),
            _ => UnauthorizedProblem()
        };
    }

    [HttpGet("me/relations")]
    [Authorize]
    [ProducesResponseType<BlockRelationsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMyBlockRelations(CancellationToken cancellationToken = default)
    {
        var result = await userBlockService.ListBlockRelationsAsync(GetUserIdOrEmpty(), cancellationToken);
        return result.Status switch
        {
            UserBlockListStatus.Success => Ok(new BlockRelationsResponse(result.BlockedProfileIds, result.BlockerProfileIds)),
            UserBlockListStatus.ProvisioningPending => ProfileProvisioningPending(),
            _ => UnauthorizedProblem()
        };
    }

    private Guid GetUserIdOrEmpty() =>
        currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty
            ? currentUserService.UserId
            : Guid.Empty;

    private ObjectResult ValidationProblem(string? errorCode)
    {
        var details = new ProblemDetails
        {
            Title = "Validation failed",
            Detail = "The block request is invalid.",
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
        detail: "Authentication is required to manage blocks.",
        statusCode: StatusCodes.Status401Unauthorized);
}
