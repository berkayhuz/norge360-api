// <copyright file="DiscoveryInternalEventOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Discovery.API.Security;

public sealed class DiscoveryInternalEventOptions
{
    public const string SectionName = "Security:InternalEvents";

    public bool Enabled { get; set; } = true;

    public string HeaderName { get; set; } = "X-Discovery-Internal-Token";

    public string? Token { get; set; }
}
