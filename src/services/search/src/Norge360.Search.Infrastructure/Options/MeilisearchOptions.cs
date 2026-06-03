// <copyright file="MeilisearchOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Infrastructure.Options;

public sealed class MeilisearchOptions
{
    public const string SectionName = "Meilisearch";
    public const string HttpClientName = "search-meilisearch";

    public string Endpoint { get; init; } = "http://localhost:7700";
    public string ApiKey { get; init; } = string.Empty;
}
