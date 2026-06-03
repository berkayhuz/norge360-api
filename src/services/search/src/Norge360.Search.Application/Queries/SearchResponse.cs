// <copyright file="SearchResponse.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Queries;

public sealed record SearchResponse(
    string Query,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<SearchResultItem> Items,
    bool PermissionPostFilteringApplied = false);

public sealed record SearchResultItem(
    string Id,
    SearchDocumentSource Source,
    string Type,
    string Title,
    string Summary,
    string Url,
    SearchDocumentVisibility Visibility,
    string Locale,
    IReadOnlyCollection<string> Tags,
    string? Username = null,
    string? DisplayName = null,
    string? AvatarUrl = null,
    string? Bio = null,
    int? FollowersCount = null,
    bool? IsVerified = null,
    double? RankingScore = null,
    string? HighlightedTitle = null,
    string? HighlightedSummary = null,
    IReadOnlyCollection<SearchHighlight>? Highlights = null);

public sealed record SearchHighlight(string Field, string Value);
