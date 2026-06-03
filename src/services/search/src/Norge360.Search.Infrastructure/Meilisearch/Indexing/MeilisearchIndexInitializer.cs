// <copyright file="MeilisearchIndexInitializer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Search.Infrastructure.Meilisearch.Client;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Infrastructure.Meilisearch.Indexing;

internal sealed class MeilisearchIndexInitializer(
    IMeilisearchDocumentClient documentClient,
    IOptions<SearchOptions> searchOptions) : IMeilisearchIndexInitializer
{
    private static readonly MeilisearchIndexConfiguration IndexConfiguration = new(
        SearchableAttributes: ["title", "summary", "content", "tags"],
        FilterableAttributes:
        [
            "source",
            "type",
            "tenantId",
            "visibility",
            "requiredPermissions",
            "permissionMatchMode",
            "locale",
            "tags",
            "isDeleted",
            "ownerUserId",
            "assignedUserIds"
        ],
        SortableAttributes: ["createdAtUtc", "updatedAtUtc", "indexedAtUtc", "boost"],
        DisplayedAttributes:
        [
            "id",
            "source",
            "type",
            "title",
            "summary",
            "url",
            "tenantId",
            "requiredPermissions",
            "visibility",
            "permissionMatchMode",
            "locale",
            "tags",
            "boost",
            "createdAtUtc",
            "updatedAtUtc",
            "indexedAtUtc",
            "isDeleted",
            "metadata",
            "externalId",
            "ownerUserId",
            "assignedUserIds",
            "sourceVersion",
            "sourceUpdatedAtUtc"
        ]);

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            var indexName = searchOptions.Value.IndexName;
            await documentClient.EnsureIndexExistsAsync(indexName, primaryKey: "id", cancellationToken);
            await documentClient.ApplyIndexConfigurationAsync(indexName, IndexConfiguration, cancellationToken);

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
