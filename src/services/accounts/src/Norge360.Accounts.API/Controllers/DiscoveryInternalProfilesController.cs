// <copyright file="DiscoveryInternalProfilesController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Route("api/accounts/internal/discovery/profiles")]
public sealed class DiscoveryInternalProfilesController(
    IProfileQueryService profileQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DiscoveryProfileExportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportProfiles(
        [FromQuery] DateTimeOffset? cursorUpdatedAt = null,
        [FromQuery] Guid? cursorProfileId = null,
        [FromQuery] int take = 250,
        CancellationToken cancellationToken = default)
    {
        var response = await profileQueryService.GetDiscoveryProfileExportBatchAsync(
            cursorUpdatedAt,
            cursorProfileId,
            take,
            cancellationToken);

        return Ok(response);
    }
}
