// <copyright file="InternalUsersController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Norge360.Accounts.API.Security;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;
using Norge360.CurrentUser;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Route("api/accounts/internal/users")]
public sealed class InternalUsersController(
    IProfileQueryService profileQueryService,
    ICommunityNotificationTargetService communityNotificationTargetService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("resolve-by-username/{username}")]
    [ProducesResponseType<InternalAuthUserResolutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveByUsername(string username, CancellationToken cancellationToken)
    {
        var authUserId = await profileQueryService.ResolveAuthUserIdByUsernameAsync(username, cancellationToken);
        if (!authUserId.HasValue)
        {
            return NotFound();
        }

        return Ok(new InternalAuthUserResolutionResponse(authUserId.Value));
    }

    [HttpGet("{authUserId:guid}/identity")]
    [ProducesResponseType<InternalUserIdentityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveIdentity(Guid authUserId, CancellationToken cancellationToken)
    {
        var userName = await profileQueryService.ResolveUsernameByAuthUserIdAsync(authUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return NotFound();
        }

        return Ok(new InternalUserIdentityResponse(userName));
    }

    [HttpGet("resolve-by-profile/{profileId:guid}")]
    [ProducesResponseType<InternalAuthUserResolutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveByProfileId(Guid profileId, CancellationToken cancellationToken)
    {
        var authUserId = await profileQueryService.ResolveAuthUserIdByProfileIdAsync(profileId, cancellationToken);
        if (!authUserId.HasValue)
        {
            return NotFound();
        }

        return Ok(new InternalAuthUserResolutionResponse(authUserId.Value));
    }

    [HttpPost("batch-summary")]
    [ProducesResponseType<InternalUserBatchSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchSummary([FromBody] InternalUserBatchSummaryRequest request, CancellationToken cancellationToken)
    {
        if (request.UserIds.Count > 100)
        {
            return BadRequest(new { errorCode = "batch_limit_exceeded", max = 100 });
        }

        var viewerAuthUserId = currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty
            ? currentUserService.UserId
            : (Guid?)null;

        var response = await profileQueryService.GetInternalUserBatchSummaryAsync(request, viewerAuthUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("community-notification-targets")]
    [ProducesResponseType<CommunityNotificationTargetsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CommunityNotificationTargets(
        [FromBody] CommunityNotificationTargetsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MaxRecipients is < 1 or > 1000)
        {
            return BadRequest(new { errorCode = "max_recipients_out_of_range", min = 1, max = 1000 });
        }

        var response = await communityNotificationTargetService.ResolveAsync(request, cancellationToken);
        return Ok(response);
    }
}
