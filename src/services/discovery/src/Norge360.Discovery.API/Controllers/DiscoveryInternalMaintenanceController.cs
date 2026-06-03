// <copyright file="DiscoveryInternalMaintenanceController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Norge360.Discovery.API.Security;
using Norge360.Discovery.API.Services;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.API.Controllers;

[ApiController]
[Route("api/discovery/internal")]
[AllowAnonymous]
public sealed class DiscoveryInternalMaintenanceController(
    IDiscoverySnapshotService snapshotService,
    IDiscoveryRankingService rankingService,
    IAccountsDiscoveryBackfillService accountsBackfillService,
    IOptions<DiscoveryInternalEventOptions> internalEventOptions,
    ILogger<DiscoveryInternalMaintenanceController> logger) : ControllerBase
{
    [HttpPost("snapshots/batch")]
    [ProducesResponseType<DiscoverySnapshotBatchUpsertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpsertSnapshots([FromBody] DiscoverySnapshotBatchRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateInternalCaller())
        {
            return InternalCallerRequired();
        }

        return Ok(await snapshotService.UpsertBatchAsync(request, cancellationToken));
    }

    [HttpPost("backfill/accounts-profiles")]
    [ProducesResponseType<DiscoveryBackfillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BackfillAccountsProfiles(
        [FromQuery] int take = 250,
        [FromQuery] int maxBatches = 10000,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateInternalCaller())
        {
            return InternalCallerRequired();
        }

        return Ok(await accountsBackfillService.BackfillAsync(take, maxBatches, cancellationToken));
    }

    [HttpPost("rankings/recompute")]
    [ProducesResponseType<DiscoveryRankingRecomputeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecomputeRankings(CancellationToken cancellationToken)
    {
        if (!ValidateInternalCaller())
        {
            return InternalCallerRequired();
        }

        await rankingService.RecomputeAsync(cancellationToken);
        return Ok(new DiscoveryRankingRecomputeResponse(true));
    }

    private bool ValidateInternalCaller()
    {
        var options = internalEventOptions.Value;
        if (!options.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.Token))
        {
            logger.LogWarning("Discovery internal maintenance token is not configured.");
            return false;
        }

        var headerName = string.IsNullOrWhiteSpace(options.HeaderName)
            ? "X-Discovery-Internal-Token"
            : options.HeaderName;
        var provided = Request.Headers[headerName].FirstOrDefault();
        return string.Equals(provided, options.Token, StringComparison.Ordinal);
    }

    private ObjectResult InternalCallerRequired() => StatusCode(
        StatusCodes.Status403Forbidden,
        new ProblemDetails
        {
            Title = "Internal caller required",
            Detail = "Discovery maintenance endpoints are only available to trusted internal services.",
            Status = StatusCodes.Status403Forbidden
        });
}
