// <copyright file="DiscoveryAccountsOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Discovery.API.Options;

public sealed class DiscoveryAccountsOptions
{
    public const string SectionName = "Services:Accounts";

    public string? BaseUrl { get; set; }
    public string? InternalTokenHeaderName { get; set; }
    public string? InternalToken { get; set; }
}
