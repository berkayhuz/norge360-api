// <copyright file="GatewayTrustedCallerOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.AspNetCore.TrustedGateway.Options;

namespace Norge360.ApiGateway.Options;

public sealed class GatewayTrustedCallerOptionsValidation(IHostEnvironment environment) : IValidateOptions<TrustedGatewayOptions>
{
    private static readonly string[] UnsafeSecretMarkers = ["REPLACE", "CHANGE_ME", "LOCAL", "DEV", "TEST"];

    public ValidateOptionsResult Validate(string? name, TrustedGatewayOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Source)) failures.Add("Security:TrustedGateway:Source is required.");
        if (string.IsNullOrWhiteSpace(options.CurrentKeyId)) failures.Add("Security:TrustedGateway:CurrentKeyId is required.");
        if (string.IsNullOrWhiteSpace(options.SignatureHeaderName)) failures.Add("Security:TrustedGateway:SignatureHeaderName is required.");
        if (string.IsNullOrWhiteSpace(options.TimestampHeaderName)) failures.Add("Security:TrustedGateway:TimestampHeaderName is required.");
        if (string.IsNullOrWhiteSpace(options.KeyIdHeaderName)) failures.Add("Security:TrustedGateway:KeyIdHeaderName is required.");
        if (string.IsNullOrWhiteSpace(options.SourceHeaderName)) failures.Add("Security:TrustedGateway:SourceHeaderName is required.");
        if (string.IsNullOrWhiteSpace(options.NonceHeaderName)) failures.Add("Security:TrustedGateway:NonceHeaderName is required.");
        if (string.IsNullOrWhiteSpace(options.ContentHashHeaderName)) failures.Add("Security:TrustedGateway:ContentHashHeaderName is required.");
        if (options.AllowedClockSkewSeconds is < 5 or > 300) failures.Add("Security:TrustedGateway:AllowedClockSkewSeconds must be between 5 and 300.");
        if (options.ReplayProtectionWindowSeconds is < 30 or > 600) failures.Add("Security:TrustedGateway:ReplayProtectionWindowSeconds must be between 30 and 600.");
        if (options.AllowedSources.Length == 0) failures.Add("Security:TrustedGateway:AllowedSources must contain at least one source.");

        if (options.Keys.Count == 0)
        {
            failures.Add("Security:TrustedGateway:Keys must contain at least one key.");
        }
        else
        {
            if (!options.Keys.Any(x => x.Enabled && x.SignRequests && string.Equals(x.KeyId, options.CurrentKeyId, StringComparison.Ordinal)))
            {
                failures.Add("Security:TrustedGateway:CurrentKeyId must point to an enabled signing key.");
            }

            foreach (var key in options.Keys)
            {
                if (string.IsNullOrWhiteSpace(key.KeyId)) failures.Add("Security:TrustedGateway:Keys:KeyId is required.");
                if (string.IsNullOrWhiteSpace(key.Secret)) failures.Add($"Security:TrustedGateway:Keys:{key.KeyId}:Secret is required.");

                if (environment.IsProduction())
                {
                    if ((key.Secret?.Length ?? 0) < 32)
                    {
                        failures.Add($"Security:TrustedGateway:Keys:{key.KeyId}:Secret must be at least 32 characters in production.");
                    }

                    if (!string.IsNullOrWhiteSpace(key.Secret) &&
                        UnsafeSecretMarkers.Any(marker => key.Secret.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    {
                        failures.Add($"Security:TrustedGateway:Keys:{key.KeyId}:Secret contains a non-production placeholder marker.");
                    }

                    if (!string.IsNullOrWhiteSpace(key.KeyId) &&
                        (key.KeyId.Contains("local", StringComparison.OrdinalIgnoreCase) ||
                         key.KeyId.Contains("dev", StringComparison.OrdinalIgnoreCase) ||
                         key.KeyId.Contains("test", StringComparison.OrdinalIgnoreCase)))
                    {
                        failures.Add($"Security:TrustedGateway:Keys:{key.KeyId}:KeyId cannot be a local/dev/test identifier in production.");
                    }
                }
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
