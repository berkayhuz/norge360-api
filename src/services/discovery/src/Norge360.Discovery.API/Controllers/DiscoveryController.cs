// <copyright file="DiscoveryController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.CurrentUser;
using Norge360.Discovery.Application.Abstractions;

namespace Norge360.Discovery.API.Controllers;

[ApiController]
[Route("api/discovery")]
public sealed class DiscoveryController(
    IDiscoveryRankingService rankingService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult GetHealth() => Ok(new { service = "discovery", status = "ok" });

    [HttpGet("popular-users")]
    public async Task<IActionResult> GetPopularUsers([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => Ok(await rankingService.GetPopularUsersAsync(limit, GetViewerUserId(), cancellationToken));

    [HttpGet("trending-users")]
    public async Task<IActionResult> GetTrendingUsers([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => Ok(await rankingService.GetTrendingUsersAsync(limit, GetViewerUserId(), cancellationToken));

    [HttpGet("follow-suggestions")]
    public async Task<IActionResult> GetFollowSuggestions([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => Ok(await rankingService.GetFollowSuggestionsAsync(limit, GetViewerUserId(), cancellationToken));

    [HttpGet("hub")]
    public async Task<IActionResult> GetHub([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => Ok(await rankingService.GetHubAsync(limit, GetViewerUserId(), cancellationToken));

    private Guid? GetViewerUserId() => currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty ? currentUserService.UserId : null;
}
