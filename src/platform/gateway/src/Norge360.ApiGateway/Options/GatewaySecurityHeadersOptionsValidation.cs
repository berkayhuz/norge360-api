// <copyright file="GatewaySecurityHeadersOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;

namespace Norge360.ApiGateway.Options;

public sealed class GatewaySecurityHeadersOptionsValidation : IValidateOptions<GatewaySecurityHeadersOptions>
{
    public ValidateOptionsResult Validate(string? name, GatewaySecurityHeadersOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ContentSecurityPolicy)) failures.Add("Security:Headers:ContentSecurityPolicy is required.");
        if (string.IsNullOrWhiteSpace(options.ReferrerPolicy)) failures.Add("Security:Headers:ReferrerPolicy is required.");
        if (string.IsNullOrWhiteSpace(options.PermissionsPolicy)) failures.Add("Security:Headers:PermissionsPolicy is required.");

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
