// <copyright file="SearchBlockFilterOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Infrastructure.Options;

public sealed class SearchBlockFilterOptions
{
    public const string SectionName = "Search:BlockFilter";

    public bool Enabled { get; set; } = true;
    public string AccountsApiBaseUrl { get; set; } = "http://localhost:5100";
}
