// <copyright file="MeilisearchSearchQueryService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Application.Filtering;
using Norge360.Search.Application.Queries;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Infrastructure.Abstractions;
using Norge360.Search.Infrastructure.Meilisearch.Client;
using Norge360.Search.Infrastructure.Meilisearch.Documents;
using Norge360.Search.Infrastructure.Meilisearch.Indexing;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Infrastructure.Meilisearch.Queries;

internal sealed class MeilisearchSearchQueryService(
    IOptions<SearchOptions> searchOptions,
    IMeilisearchIndexInitializer indexInitializer,
    IMeilisearchDocumentClient documentClient,
    MeilisearchFilterBuilder filterBuilder,
    MeilisearchDocumentMapper mapper,
    IBlockedProfileIdsProvider blockedProfileIdsProvider,
    ILogger<MeilisearchSearchQueryService> logger) : ISearchQueryService
{
    public async Task<SearchResponse> SearchAsync(
        SearchRequest request,
        SearchAccessContext accessContext,
        CancellationToken cancellationToken)
    {
        if (!searchOptions.Value.Provider.Equals("Meilisearch", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Search provider '{searchOptions.Value.Provider}' is not supported by {nameof(MeilisearchSearchQueryService)}.");
        }

        var plan = SearchFilterPlanBuilder.Build(request, accessContext);
        await indexInitializer.EnsureInitializedAsync(cancellationToken);

        var filter = filterBuilder.Build(plan);
        var providerRequest = new MeilisearchDocumentSearchRequest(
            IndexName: searchOptions.Value.IndexName,
            Query: plan.Query,
            Filter: filter,
            Page: plan.Page,
            PageSize: plan.PageSize,
            Sort: plan.Sort);

        var page = await documentClient.SearchAsync(providerRequest, cancellationToken);

        var blockedProfileIds = await ResolveBlockedProfileIdsAsync(accessContext, cancellationToken);
        var visibleItems = new List<(SearchResultItem Item, MeilisearchSearchDocument Stored)>(page.Documents.Count);
        foreach (var storedDocument in page.Documents)
        {
            SearchDocument document;
            try
            {
                document = mapper.ToSearchDocument(storedDocument);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Skipping malformed search document '{DocumentId}'.", storedDocument.Id);
                continue;
            }

            if (!SearchDocumentVisibilityEvaluator.CanAccess(document, accessContext))
            {
                continue;
            }

            if (IsBlockedByProfileRelation(storedDocument, blockedProfileIds))
            {
                continue;
            }

            visibleItems.Add((mapper.ToSearchResultItem(storedDocument), storedDocument));
        }

        var rankedItems = RankItems(plan, visibleItems);

        // We return only post-filtered counts to avoid leaking hidden document counts.
        var safeTotalCount = rankedItems.Count;

        return new SearchResponse(
            Query: plan.Query,
            Page: plan.Page,
            PageSize: plan.PageSize,
            TotalCount: safeTotalCount,
            Items: rankedItems,
            PermissionPostFilteringApplied: plan.RequiresPermissionPostFiltering || plan.RequiresVisibilityPostFiltering);
    }

    private async Task<IReadOnlySet<Guid>> ResolveBlockedProfileIdsAsync(
        SearchAccessContext accessContext,
        CancellationToken cancellationToken)
    {
        if (!accessContext.IsAuthenticated || !accessContext.UserId.HasValue || accessContext.UserId.Value == Guid.Empty)
        {
            return new HashSet<Guid>();
        }

        return await blockedProfileIdsProvider.GetRelatedBlockedProfileIdsAsync(accessContext.UserId.Value, cancellationToken);
    }

    private static bool IsBlockedByProfileRelation(
        MeilisearchSearchDocument storedDocument,
        IReadOnlySet<Guid> blockedProfileIds)
    {
        if (blockedProfileIds.Count == 0)
        {
            return false;
        }

        var profileIdRaw = ReadMetadata(storedDocument, "profileId");
        return Guid.TryParse(profileIdRaw, out var profileId) && blockedProfileIds.Contains(profileId);
    }

    private static IReadOnlyCollection<SearchResultItem> RankItems(
        SearchFilterPlan plan,
        IReadOnlyCollection<(SearchResultItem Item, MeilisearchSearchDocument Stored)> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(plan.Query))
        {
            return items.Select(x => x.Item).ToArray();
        }

        var normalizedQuery = plan.Query.Trim().ToLowerInvariant();
        var shouldApplyUserRanking = string.Equals(plan.Type, "user", StringComparison.OrdinalIgnoreCase) ||
                                     plan.EffectiveSources.Any(x => x == SearchDocumentSource.Forum);
        if (!shouldApplyUserRanking)
        {
            return items.Select(x => x.Item).ToArray();
        }

        return items
            .Select(x => new
            {
                x.Item,
                Score = ComputeUserRankingScore(normalizedQuery, x.Stored)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.RankingScore ?? 0)
            .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Item)
            .ToArray();
    }

    private static double ComputeUserRankingScore(string query, MeilisearchSearchDocument document)
    {
        var username = ReadMetadata(document, "normalizedUsername", "username").ToLowerInvariant();
        var displayName = ReadMetadata(document, "displayName").ToLowerInvariant();
        var isVerified = string.Equals(ReadMetadata(document, "isVerified"), "true", StringComparison.OrdinalIgnoreCase);
        var followers = ReadIntMetadata(document, "followersCount");

        double score = 0;

        if (!string.IsNullOrWhiteSpace(username))
        {
            if (string.Equals(username, query, StringComparison.Ordinal))
            {
                score += 100;
            }
            else if (username.StartsWith(query, StringComparison.Ordinal))
            {
                score += 80;
            }
            else if (username.Contains(query, StringComparison.Ordinal))
            {
                score += 40;
            }
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            if (string.Equals(displayName, query, StringComparison.Ordinal))
            {
                score += 70;
            }
            else if (displayName.StartsWith(query, StringComparison.Ordinal))
            {
                score += 50;
            }
            else if (displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Any(part => part.StartsWith(query, StringComparison.Ordinal)))
            {
                score += 45;
            }
        }

        var fuzzyDistance = MinDistance(query, username, displayName);
        if (fuzzyDistance <= 1)
        {
            score += 35;
        }
        else if (fuzzyDistance == 2)
        {
            score += 20;
        }

        if (isVerified)
        {
            score += 15;
        }

        if (followers > 0)
        {
            score += Math.Min(20, Math.Log10(followers + 1) * 5);
        }

        score += document.Boost * 10;
        return score;
    }

    private static int MinDistance(string query, params string[] values)
    {
        var min = int.MaxValue;
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var distance = LevenshteinDistance(query, value);
            if (distance < min)
            {
                min = distance;
            }
        }

        return min == int.MaxValue ? 999 : min;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var rows = left.Length + 1;
        var cols = right.Length + 1;
        var matrix = new int[rows, cols];

        for (var i = 0; i < rows; i++) matrix[i, 0] = i;
        for (var j = 0; j < cols; j++) matrix[0, j] = j;

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[rows - 1, cols - 1];
    }

    private static int ReadIntMetadata(MeilisearchSearchDocument document, string key)
    {
        var raw = ReadMetadata(document, key);
        return int.TryParse(raw, out var parsed) ? parsed : 0;
    }

    private static string ReadMetadata(MeilisearchSearchDocument document, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (document.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
