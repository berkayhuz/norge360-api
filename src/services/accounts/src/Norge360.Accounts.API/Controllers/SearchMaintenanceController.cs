// <copyright file="SearchMaintenanceController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Route("api/accounts/search-maintenance")]
public sealed class SearchMaintenanceController(
    IUserSearchReindexService userSearchReindexService) : ControllerBase
{
    [HttpPost("reindex-users")]
    [Authorize(Roles = "admin,administrator")]
    [ProducesResponseType<UserSearchReindexResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReindexUsers(
        [FromQuery] int batchSize = 250,
        CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 10, 500);
        var enqueuedCount = await userSearchReindexService.EnqueueAllActiveUsersAsync(safeBatchSize, cancellationToken);
        return Ok(new UserSearchReindexResponse(enqueuedCount, safeBatchSize));
    }
}
