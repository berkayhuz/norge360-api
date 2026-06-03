// <copyright file="AccountsTrustedGatewayOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.AspNetCore.Security;
using Norge360.AspNetCore.TrustedGateway.Options;

namespace Norge360.Accounts.API.Security;

public sealed class AccountsTrustedGatewayOptionsValidation(IHostEnvironment environment) : IValidateOptions<TrustedGatewayOptions>
{
    public ValidateOptionsResult Validate(string? name, TrustedGatewayOptions options)
    {
        if (!options.RequireTrustedGateway)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (options.Keys.Count == 0) failures.Add("Security:TrustedGateway:Keys must contain at least one key.");
        if (options.AllowedSources.Length == 0) failures.Add("Security:TrustedGateway:AllowedSources must contain at least one source.");
        if (string.IsNullOrWhiteSpace(options.CurrentKeyId)) failures.Add("Security:TrustedGateway:CurrentKeyId is required.");
        if (options.AllowedClockSkewSeconds is < 5 or > 300) failures.Add("Security:TrustedGateway:AllowedClockSkewSeconds must be between 5 and 300.");
        if (options.ReplayProtectionWindowSeconds is < 30 or > 600) failures.Add("Security:TrustedGateway:ReplayProtectionWindowSeconds must be between 30 and 600.");

        foreach (var proxy in options.AllowedGatewayProxies)
        {
            if (!System.Net.IPAddress.TryParse(proxy, out _))
            {
                failures.Add($"Security:TrustedGateway:AllowedGatewayProxies contains invalid IP '{proxy}'.");
            }
        }

        foreach (var network in options.AllowedGatewayNetworks)
        {
            if (!SecuritySupport.TryParseNetwork(network, out _))
            {
                failures.Add($"Security:TrustedGateway:AllowedGatewayNetworks contains invalid CIDR '{network}'.");
            }
        }

        if (environment.IsProduction() && options.AllowedGatewayProxies.Length == 0 && options.AllowedGatewayNetworks.Length == 0)
        {
            failures.Add("Security:TrustedGateway must define at least one allowed gateway proxy or network in production.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
