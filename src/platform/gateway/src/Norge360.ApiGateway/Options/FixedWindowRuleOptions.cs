// <copyright file="FixedWindowRuleOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Threading.RateLimiting;

namespace Norge360.ApiGateway.Options;

public sealed record FixedWindowRuleOptions(int PermitLimit, int WindowSeconds, int QueueLimit)
{
    public FixedWindowRateLimiterOptions ToLimiterOptions() => new()
    {
        PermitLimit = PermitLimit,
        Window = TimeSpan.FromSeconds(WindowSeconds),
        QueueLimit = QueueLimit,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    };
}
