// <copyright file="SearchBootstrapOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Worker.Options;

public sealed class SearchBootstrapOptions
{
    public const string SectionName = "SearchBootstrap";
    public bool ReindexUsersOnStartup { get; set; }
    public int BatchSize { get; set; } = 250;
}
