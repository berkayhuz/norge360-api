// <copyright file="InternalServiceSigningOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.API.Options;

public sealed class InternalServiceSigningOptions
{
    public bool Enabled { get; set; }
    public string Secret { get; set; } = string.Empty;
    public int ClockSkewSeconds { get; set; } = 120;
    public string ServiceName { get; set; } = "community-api";
}
