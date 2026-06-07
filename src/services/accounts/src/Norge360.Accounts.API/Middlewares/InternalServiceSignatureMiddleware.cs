// <copyright file="InternalServiceSignatureMiddleware.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.API.Security;

namespace Norge360.Accounts.API.Middlewares;

public sealed class InternalServiceSignatureMiddleware(
    RequestDelegate next,
    IInternalServiceRequestValidator validator,
    IConfiguration configuration,
    ILogger<InternalServiceSignatureMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) &&
            (context.Request.Path.Equals("/api/accounts/internal/users/batch-summary", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/api/accounts/internal/users/community-notification-targets", StringComparison.OrdinalIgnoreCase)))
        {
            if (!await validator.ValidateAsync(context.Request, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { errorCode = "internal_signature_invalid" }, context.RequestAborted);
                return;
            }
        }

        if (HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/accounts/internal/discovery/profiles", StringComparison.OrdinalIgnoreCase))
        {
            if (!await ValidateDiscoveryExportRequestAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { errorCode = "internal_discovery_token_invalid" }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }

    private async Task<bool> ValidateDiscoveryExportRequestAsync(HttpContext context)
    {
        var token = configuration["Services:Discovery:InternalToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            var headerName = configuration["Services:Discovery:InternalTokenHeaderName"];
            headerName = string.IsNullOrWhiteSpace(headerName) ? "X-Discovery-Internal-Token" : headerName;

            var provided = context.Request.Headers[headerName].FirstOrDefault();
            var valid = string.Equals(provided, token, StringComparison.Ordinal);
            if (!valid)
            {
                logger.LogWarning("Accounts discovery profile export rejected: invalid internal token.");
            }

            return valid;
        }

        return await validator.ValidateAsync(context.Request, context.RequestAborted);
    }
}
