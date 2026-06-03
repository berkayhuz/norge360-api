// <copyright file="GatewayForwardedHeadersOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.AspNetCore.Security;

namespace Norge360.ApiGateway.Options;

public sealed class GatewayForwardedHeadersOptionsValidation(IHostEnvironment environment)
    : IValidateOptions<GatewayForwardedHeadersOptions>
{
    public ValidateOptionsResult Validate(string? name, GatewayForwardedHeadersOptions options)
    {
        var failures = new List<string>();

        foreach (var network in options.KnownNetworks)
        {
            if (!SecuritySupport.TryParseNetwork(network, out _))
            {
                failures.Add($"Security:ForwardedHeaders:KnownNetworks contains invalid CIDR '{network}'.");
            }
        }

        foreach (var proxy in options.KnownProxies)
        {
            if (!System.Net.IPAddress.TryParse(proxy, out _))
            {
                failures.Add($"Security:ForwardedHeaders:KnownProxies contains invalid IP '{proxy}'.");
            }
        }

        if (environment.IsProduction() && options.KnownNetworks.Length == 0 && options.KnownProxies.Length == 0)
        {
            failures.Add("Security:ForwardedHeaders must define at least one known proxy or known network in production.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}

