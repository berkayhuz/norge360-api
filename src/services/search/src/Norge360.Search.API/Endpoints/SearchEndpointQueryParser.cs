// <copyright file="SearchEndpointQueryParser.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Norge360.Search.Application.Localization;
using Norge360.Search.Application.Queries;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.API.Endpoints;

internal static class SearchEndpointQueryParser
{
    private static readonly char[] Delimiters = [',', ';'];
    private const int SuggestMinQueryLength = 1;

    public static bool TryParse(
        IQueryCollection queryCollection,
        out SearchRequest request,
        out string? error)
    {
        var query = FirstNonEmpty(queryCollection, "q") ?? FirstNonEmpty(queryCollection, "query");

        if (!TryParseInt(queryCollection, "page", out var page, out error) ||
            !TryParseInt(queryCollection, "pageSize", out var pageSize, out error))
        {
            request = default!;
            return false;
        }

        if (!TryParseSources(queryCollection, out var sources, out error))
        {
            request = default!;
            return false;
        }

        var type = FirstNonEmpty(queryCollection, "type");
        var locale = SearchLocale.CanonicalizeRequestedLocale(FirstNonEmpty(queryCollection, "locale"));
        var tags = ReadStringValues(queryCollection, "tags");
        var sort = FirstNonEmpty(queryCollection, "sort");

        request = new SearchRequest(
            Query: query,
            Sources: sources,
            Type: type,
            Locale: locale,
            Tags: tags,
            Page: page,
            PageSize: pageSize,
            IncludeDeleted: false,
            Sort: sort);

        error = null;
        return true;
    }

    public static bool TryParseSuggest(
        IQueryCollection queryCollection,
        out SearchRequest request,
        out string? error)
    {
        var query = FirstNonEmpty(queryCollection, "q") ?? FirstNonEmpty(queryCollection, "query");
        if (string.IsNullOrWhiteSpace(query))
        {
            request = default!;
            error = "Query parameter 'q' is required for suggestions.";
            return false;
        }

        query = query.Trim();
        if (query.Length < SuggestMinQueryLength)
        {
            request = default!;
            error = $"Query parameter 'q' must be at least {SuggestMinQueryLength} character(s).";
            return false;
        }

        var pageSize = query.Length switch
        {
            <= 1 => 5,
            2 => 8,
            _ => 12
        };

        request = new SearchRequest(
            Query: query,
            Sources: [SearchDocumentSource.Forum],
            Type: "user",
            Locale: SearchLocale.CanonicalizeRequestedLocale(FirstNonEmpty(queryCollection, "locale")),
            Tags: null,
            Page: 1,
            PageSize: pageSize,
            IncludeDeleted: false,
            Sort: null);

        error = null;
        return true;
    }

    private static bool TryParseInt(
        IQueryCollection queryCollection,
        string key,
        out int? value,
        out string? error)
    {
        value = null;
        error = null;

        var raw = FirstNonEmpty(queryCollection, key);
        if (raw is null)
        {
            return true;
        }

        if (int.TryParse(raw, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Query parameter '{key}' must be an integer.";
        return false;
    }

    private static bool TryParseSources(
        IQueryCollection queryCollection,
        out IReadOnlyCollection<SearchDocumentSource>? sources,
        out string? error)
    {
        var rawValues = ReadStringValues(queryCollection, "source", "sources");
        if (rawValues is null || rawValues.Count == 0)
        {
            sources = null;
            error = null;
            return true;
        }

        var parsed = new List<SearchDocumentSource>(rawValues.Count);
        foreach (var rawValue in rawValues)
        {
            if (Enum.TryParse<SearchDocumentSource>(rawValue, ignoreCase: true, out var source))
            {
                parsed.Add(source);
                continue;
            }

            sources = null;
            error =
                $"Unsupported source '{rawValue}'. Allowed values: {string.Join(", ", Enum.GetNames<SearchDocumentSource>())}.";
            return false;
        }

        sources = parsed
            .Distinct()
            .ToArray();
        error = null;
        return true;
    }

    private static string? FirstNonEmpty(IQueryCollection queryCollection, string key)
    {
        if (!queryCollection.TryGetValue(key, out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static IReadOnlyCollection<string>? ReadStringValues(IQueryCollection queryCollection, params string[] keys)
    {
        var values = new List<string>();
        foreach (var key in keys)
        {
            if (!queryCollection.TryGetValue(key, out var queryValues))
            {
                continue;
            }

            values.AddRange(SplitValues(queryValues));
        }

        if (values.Count == 0)
        {
            return null;
        }

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> SplitValues(StringValues rawValues) =>
        rawValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => (value ?? string.Empty).Split(Delimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim());
}
