// <copyright file="TrustedGatewayMiddleware.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Norge360.AspNetCore.RequestContext;
using Norge360.AspNetCore.TrustedGateway.Abstractions;
using Norge360.AspNetCore.TrustedGateway.Options;

namespace Norge360.Community.API.Middlewares;

public sealed class TrustedGatewayMiddleware(
    RequestDelegate next,
    IOptions<TrustedGatewayOptions> options,
    ITrustedGatewayRequestValidator validator,
    ILogger<TrustedGatewayMiddleware> logger)
{
    public const string TrustedGatewayValidatedItemName = "TrustedGatewayValidated";

    public async Task InvokeAsync(HttpContext context)
    {
        var trustedGatewayOptions = options.Value;
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

        if (!trustedGatewayOptions.RequireTrustedGateway || allowAnonymous || context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var correlationId = RequestContextSupport.GetOrCreateCorrelationId(context);
        var validationResult = await validator.ValidateAsync(context, correlationId, context.RequestAborted);
        if (!validationResult.Succeeded)
        {
            logger.LogWarning(
                "Trusted gateway validation rejected request for {Path}. Reason={Reason} CorrelationId={CorrelationId}",
                context.Request.Path,
                validationResult.FailureReason,
                correlationId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(
                new
                {
                    type = "https://norge360.dev/problems/trusted-gateway-required",
                    title = "Trusted caller required",
                    status = StatusCodes.Status403Forbidden,
                    detail = "The community service only accepts this endpoint via a trusted gateway.",
                    errorCode = validationResult.ErrorCode ?? "trusted_gateway_required"
                },
                context.RequestAborted);
            return;
        }

        context.Items[TrustedGatewayValidatedItemName] = true;
        await next(context);
    }
}
