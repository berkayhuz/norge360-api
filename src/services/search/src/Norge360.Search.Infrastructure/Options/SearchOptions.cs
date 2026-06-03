// <copyright file="SearchOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Infrastructure.Options;

public sealed class SearchOptions
{
    public const string SectionName = "Search";

    public string Provider { get; init; } = "Meilisearch";
    public string IndexName { get; init; } = "searchdocuments";
    public SearchStaticIndexingOptions StaticIndexing { get; init; } = new();
}

public sealed class SearchStaticIndexingOptions
{
    public bool Enabled { get; init; }
    public bool SeedOnStartup { get; init; }
    public int StartupSeedMaxAttempts { get; init; } = 5;
    public int StartupSeedRetryDelaySeconds { get; init; } = 2;
}
