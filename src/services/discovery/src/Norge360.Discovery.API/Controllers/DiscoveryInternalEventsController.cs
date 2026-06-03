// <copyright file="DiscoveryInternalEventsController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Norge360.Discovery.API.Security;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Contracts.Requests;

namespace Norge360.Discovery.API.Controllers;

[ApiController]
[Route("api/discovery/internal/events")]
[AllowAnonymous]
public sealed class DiscoveryInternalEventsController(
    IDiscoveryEventIngestionService ingestionService,
    IOptions<DiscoveryInternalEventOptions> internalEventOptions,
    ILogger<DiscoveryInternalEventsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] DiscoveryEventRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateInternalCaller())
        {
            return InternalCallerRequired();
        }

        return Ok(await ingestionService.IngestAsync(request, cancellationToken));
    }

    [HttpPost("batch")]
    public async Task<IActionResult> IngestBatch([FromBody] DiscoveryEventBatchRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateInternalCaller())
        {
            return InternalCallerRequired();
        }

        return Ok(await ingestionService.IngestBatchAsync(request, cancellationToken));
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
            logger.LogWarning("Discovery internal event token is not configured.");
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
            Detail = "Discovery event ingestion is only available to trusted internal services.",
            Status = StatusCodes.Status403Forbidden
        });
}
