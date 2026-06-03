// <copyright file="SearchRequest.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Queries;

public sealed record SearchRequest(
    string? Query = null,
    IReadOnlyCollection<SearchDocumentSource>? Sources = null,
    string? Type = null,
    string? Locale = null,
    IReadOnlyCollection<string>? Tags = null,
    int? Page = null,
    int? PageSize = null,
    bool IncludeDeleted = false,
    string? Sort = null);

public static class SearchRequestDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
