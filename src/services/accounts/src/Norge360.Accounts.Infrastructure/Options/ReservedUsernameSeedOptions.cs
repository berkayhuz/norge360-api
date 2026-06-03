// <copyright file="ReservedUsernameSeedOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Infrastructure.Options;

public sealed class ReservedUsernameSeedOptions
{
    public const string SectionName = "Accounts";

    public List<ReservedUsernameSeedEntry> ReservedUsernames { get; init; } = [];
}

public sealed class ReservedUsernameSeedEntry
{
    public string Value { get; init; } = string.Empty;

    public string? Reason { get; init; }
}
