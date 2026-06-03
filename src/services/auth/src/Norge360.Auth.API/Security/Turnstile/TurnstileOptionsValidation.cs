// <copyright file="TurnstileOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;

namespace Norge360.Auth.API.Security.Turnstile;

public sealed class TurnstileOptionsValidation(IHostEnvironment environment) : IValidateOptions<TurnstileOptions>
{
    private static readonly string[] KnownCloudflareTestKeys =
    [
        "1x00000000000000000000AA",
        "2x00000000000000000000AB",
        "3x00000000000000000000FF"
    ];

    public ValidateOptionsResult Validate(string? name, TurnstileOptions options)
    {
        var failures = new List<string>();
        var allowedHostnames = options.AllowedHostnames
            .Where(static hostname => !string.IsNullOrWhiteSpace(hostname))
            .Select(static hostname => hostname.Trim())
            .ToArray();

        if (!options.Enabled && environment.IsProduction())
        {
            failures.Add("Cloudflare:Turnstile:Enabled cannot be false in production.");
        }

        if (options.Enabled && string.IsNullOrWhiteSpace(options.SecretKey) && environment.IsProduction())
        {
            failures.Add("Cloudflare:Turnstile:SecretKey is required in production.");
        }

        if (allowedHostnames.Length == 0)
        {
            failures.Add("Cloudflare:Turnstile:AllowedHostnames must contain at least one hostname.");
        }

        if (environment.IsProduction())
        {
            if (KnownCloudflareTestKeys.Contains(options.SecretKey, StringComparer.Ordinal))
            {
                failures.Add("Cloudflare:Turnstile:SecretKey cannot be a known Cloudflare test key in production.");
            }

            if (allowedHostnames.Any(hostname =>
                    hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    hostname.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add("Cloudflare:Turnstile:AllowedHostnames cannot contain localhost or 127.0.0.1 in production.");
            }

            if (!allowedHostnames.Any(hostname =>
                    hostname.Equals("norge360.com", StringComparison.OrdinalIgnoreCase) ||
                    hostname.Equals("auth.norge360.com", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add("Cloudflare:Turnstile:AllowedHostnames must include norge360.com or auth.norge360.com in production.");
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
