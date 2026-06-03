// <copyright file="GatewayCorsOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.AspNetCore.Security;
using Norge360.AspNetCore.TrustedGateway.Options;

namespace Norge360.ApiGateway.Options;

public sealed class GatewayCorsOptionsValidation(IHostEnvironment? environment = null) : IValidateOptions<GatewayCorsOptions>
{
    public ValidateOptionsResult Validate(string? name, GatewayCorsOptions options)
    {
        var failures = new List<string>();
        var allowHttpLoopback = environment?.IsDevelopment() ?? true;

        if (options.AllowedOrigins.Length == 0)
        {
            failures.Add("Security:Cors:AllowedOrigins must contain at least one origin.");
        }

        foreach (var origin in options.AllowedOrigins)
        {
            if (!SecuritySupport.IsValidOrigin(origin, allowHttpForLocalhostOnly: allowHttpLoopback))
            {
                failures.Add($"Security:Cors:AllowedOrigins contains invalid origin '{origin}'.");
            }

            if (environment?.IsProduction() == true &&
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                uri.IsLoopback)
            {
                failures.Add($"Security:Cors:AllowedOrigins cannot contain loopback origin '{origin}' in production.");
            }
        }

        if (options.AllowCredentials && options.AllowedOrigins.Contains("*", StringComparer.Ordinal))
        {
            failures.Add("Security:Cors:AllowedOrigins cannot contain '*' when credentials are enabled.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
