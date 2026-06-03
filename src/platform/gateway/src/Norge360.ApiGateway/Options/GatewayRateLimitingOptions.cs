// <copyright file="GatewayRateLimitingOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Threading.RateLimiting;

namespace Norge360.ApiGateway.Options;

public sealed class GatewayRateLimitingOptions
{
    public const string SectionName = "Security:RateLimiting";
    public const string ProxyPolicyName = "gateway-proxy";

    public FixedWindowRuleOptions Global { get; set; } = new(PermitLimit: 200, WindowSeconds: 10, QueueLimit: 0);

    public FixedWindowRuleOptions Proxy { get; set; } = new(PermitLimit: 120, WindowSeconds: 10, QueueLimit: 0);
}
