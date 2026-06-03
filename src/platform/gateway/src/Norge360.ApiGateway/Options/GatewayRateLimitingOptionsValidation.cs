// <copyright file="GatewayRateLimitingOptionsValidation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;

namespace Norge360.ApiGateway.Options;

public sealed class GatewayRateLimitingOptionsValidation : IValidateOptions<GatewayRateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, GatewayRateLimitingOptions options)
    {
        var failures = new List<string>();
        ValidateRule(options.Global, "Security:RateLimiting:Global", failures);
        ValidateRule(options.Proxy, "Security:RateLimiting:Proxy", failures);
        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    private static void ValidateRule(FixedWindowRuleOptions rule, string prefix, ICollection<string> failures)
    {
        if (rule.PermitLimit <= 0) failures.Add($"{prefix}:PermitLimit must be greater than 0.");
        if (rule.WindowSeconds <= 0) failures.Add($"{prefix}:WindowSeconds must be greater than 0.");
        if (rule.QueueLimit < 0) failures.Add($"{prefix}:QueueLimit must be 0 or greater.");
    }
}
