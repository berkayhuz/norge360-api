// <copyright file="UsernameNormalizer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;

namespace Norge360.Accounts.Application.Services;

public sealed class UsernameNormalizer : IUsernameNormalizer
{
    public string Normalize(string? username) =>
        string.IsNullOrWhiteSpace(username)
            ? string.Empty
            : username.Trim().ToUpperInvariant();
}
