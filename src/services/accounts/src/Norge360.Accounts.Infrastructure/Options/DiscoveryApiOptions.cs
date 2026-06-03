// <copyright file="DiscoveryApiOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Infrastructure.Options;

public sealed class DiscoveryApiOptions
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "http://localhost:5302";

    public string InternalTokenHeaderName { get; set; } = "X-Discovery-Internal-Token";

    public string? InternalToken { get; set; }
}
