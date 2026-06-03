// <copyright file="TurnstileOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Auth.API.Security.Turnstile;

public sealed class TurnstileOptions
{
    public const string SectionName = "Cloudflare:Turnstile";

    public bool Enabled { get; set; } = true;

    public string[] AllowedHostnames { get; set; } = [];

    public string SecretKey { get; set; } = string.Empty;
}
