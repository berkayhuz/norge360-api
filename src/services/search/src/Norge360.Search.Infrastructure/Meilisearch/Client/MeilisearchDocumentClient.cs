// <copyright file="MeilisearchDocumentClient.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Meilisearch;
using Microsoft.Extensions.Logging;
using Norge360.Search.Infrastructure.Meilisearch.Documents;

namespace Norge360.Search.Infrastructure.Meilisearch.Client;

internal sealed class MeilisearchDocumentClient(
    MeilisearchClient meilisearchClient,
    ILogger<MeilisearchDocumentClient> logger) : IMeilisearchDocumentClient
{
    private const double WaitTimeoutMilliseconds = 30_000;
    private const int WaitIntervalMilliseconds = 100;

    public async Task EnsureIndexExistsAsync(string indexName, string primaryKey, CancellationToken cancellationToken)
    {
        try
        {
            await meilisearchClient.GetIndexAsync(indexName);
            return;
        }
        catch (Exception exception) when (LooksLikeIndexNotFound(exception))
        {
            logger.LogInformation("Creating missing Meilisearch index '{IndexName}' with primary key '{PrimaryKey}'.", indexName, primaryKey);
        }

        var createTask = await meilisearchClient.CreateIndexAsync(indexName, primaryKey);
        await WaitForTaskAsync(createTask.TaskUid, cancellationToken);
    }

    public async Task ApplyIndexConfigurationAsync(string indexName, MeilisearchIndexConfiguration configuration, CancellationToken cancellationToken)
    {
        var index = meilisearchClient.Index(indexName);

        var searchableTask = await index.UpdateSearchableAttributesAsync(configuration.SearchableAttributes.ToArray());
        await WaitForTaskAsync(searchableTask.TaskUid, cancellationToken);

        var filterableTask = await index.UpdateFilterableAttributesAsync(configuration.FilterableAttributes.ToArray());
        await WaitForTaskAsync(filterableTask.TaskUid, cancellationToken);

        var sortableTask = await index.UpdateSortableAttributesAsync(configuration.SortableAttributes.ToArray());
        await WaitForTaskAsync(sortableTask.TaskUid, cancellationToken);

        var displayedTask = await index.UpdateDisplayedAttributesAsync(configuration.DisplayedAttributes.ToArray());
        await WaitForTaskAsync(displayedTask.TaskUid, cancellationToken);
    }

    public async Task<MeilisearchDocumentSearchPage> SearchAsync(MeilisearchDocumentSearchRequest request, CancellationToken cancellationToken)
    {
        var index = meilisearchClient.Index(request.IndexName);
        var offset = (request.Page - 1) * request.PageSize;

        var query = new SearchQuery
        {
            Filter = request.Filter,
            Limit = request.PageSize,
            Offset = offset
        };

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            query.Sort = [request.Sort];
        }

        var searchResult = await index.SearchAsync<MeilisearchSearchDocument>(request.Query, query);
        var total = searchResult switch
        {
            SearchResult<MeilisearchSearchDocument> result => result.EstimatedTotalHits,
            PaginatedSearchResult<MeilisearchSearchDocument> paginated => paginated.TotalHits,
            _ => searchResult.Hits.Count
        };

        return new MeilisearchDocumentSearchPage(searchResult.Hits, total);
    }

    public async Task UpsertAsync(string indexName, MeilisearchSearchDocument document, CancellationToken cancellationToken)
    {
        var index = meilisearchClient.Index(indexName);
        var task = await index.AddDocumentsAsync([document]);
        await WaitForTaskAsync(task.TaskUid, cancellationToken);
    }

    public async Task UpsertManyAsync(string indexName, IReadOnlyCollection<MeilisearchSearchDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return;
        }

        var index = meilisearchClient.Index(indexName);
        var task = await index.AddDocumentsAsync(documents.ToArray());
        await WaitForTaskAsync(task.TaskUid, cancellationToken);
    }

    public async Task<MeilisearchSearchDocument?> GetDocumentAsync(string indexName, string documentId, CancellationToken cancellationToken)
    {
        var index = meilisearchClient.Index(indexName);

        try
        {
            return await index.GetDocumentAsync<MeilisearchSearchDocument>(documentId);
        }
        catch (Exception exception) when (LooksLikeDocumentNotFound(exception))
        {
            logger.LogDebug("Document '{DocumentId}' was not found in index '{IndexName}'.", documentId, indexName);
            return null;
        }
    }

    public async Task HardDeleteAsync(string indexName, string documentId, CancellationToken cancellationToken)
    {
        var index = meilisearchClient.Index(indexName);
        var task = await index.DeleteOneDocumentAsync(documentId);
        await WaitForTaskAsync(task.TaskUid, cancellationToken);
    }

    private async Task WaitForTaskAsync(int taskUid, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var taskResource = await meilisearchClient.WaitForTaskAsync(
            taskUid,
            WaitTimeoutMilliseconds,
            WaitIntervalMilliseconds,
            cancellationToken);

        EnsureTaskCompletedSuccessfully(taskUid, taskResource.Status, taskResource.Error);

        logger.LogDebug(
            "Meilisearch task {TaskUid} completed with status {Status}.",
            taskUid,
            taskResource.Status);
    }

    internal static void EnsureTaskCompletedSuccessfully(
        int taskUid,
        TaskInfoStatus status,
        IReadOnlyDictionary<string, string>? error)
    {
        if (status is not (TaskInfoStatus.Failed or TaskInfoStatus.Canceled))
        {
            return;
        }

        var errorDetails = error is { Count: > 0 }
            ? string.Join(", ", error.Select(pair => $"{pair.Key}={pair.Value}"))
            : "no error details";

        throw new InvalidOperationException(
            $"Meilisearch task {taskUid} finished with status '{status}'. Details: {errorDetails}.");
    }

    private static bool LooksLikeIndexNotFound(Exception exception)
    {
        var text = exception.ToString();
        return text.Contains("index_not_found", StringComparison.OrdinalIgnoreCase)
            || text.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeDocumentNotFound(Exception exception)
    {
        var text = exception.ToString();
        return text.Contains("document_not_found", StringComparison.OrdinalIgnoreCase)
            || text.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }
}
