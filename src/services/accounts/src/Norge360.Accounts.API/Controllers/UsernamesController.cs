// <copyright file="UsernamesController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Route("api/accounts/usernames")]
public sealed class UsernamesController(
    IUsernameAvailabilityService usernameAvailabilityService) : ControllerBase
{
    private const string UsernameAvailabilityRateLimitPolicyName = "username-availability";

    [HttpGet("check")]
    [AllowAnonymous]
    [EnableRateLimiting(UsernameAvailabilityRateLimitPolicyName)]
    [ProducesResponseType<UsernameAvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Check([FromQuery] string? username, CancellationToken cancellationToken)
    {
        if (!Request.Query.ContainsKey("username"))
        {
            return Problem(
                title: "Username query parameter is required",
                detail: "The username query parameter must be provided.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var response = await usernameAvailabilityService.CheckAsync(
            username,
            excludingProfileId: null,
            cancellationToken);

        return Ok(response);
    }
}
