// <copyright file="IMeilisearchDocumentClient.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Infrastructure.Meilisearch.Documents;

namespace Norge360.Search.Infrastructure.Meilisearch.Client;

internal interface IMeilisearchDocumentClient
{
    Task EnsureIndexExistsAsync(string indexName, string primaryKey, CancellationToken cancellationToken);

    Task ApplyIndexConfigurationAsync(string indexName, MeilisearchIndexConfiguration configuration, CancellationToken cancellationToken);

    Task<MeilisearchDocumentSearchPage> SearchAsync(MeilisearchDocumentSearchRequest request, CancellationToken cancellationToken);

    Task UpsertAsync(string indexName, MeilisearchSearchDocument document, CancellationToken cancellationToken);

    Task UpsertManyAsync(string indexName, IReadOnlyCollection<MeilisearchSearchDocument> documents, CancellationToken cancellationToken);

    Task<MeilisearchSearchDocument?> GetDocumentAsync(string indexName, string documentId, CancellationToken cancellationToken);

    Task HardDeleteAsync(string indexName, string documentId, CancellationToken cancellationToken);
}

internal sealed record MeilisearchIndexConfiguration(
    IReadOnlyCollection<string> SearchableAttributes,
    IReadOnlyCollection<string> FilterableAttributes,
    IReadOnlyCollection<string> SortableAttributes,
    IReadOnlyCollection<string> DisplayedAttributes);

internal sealed record MeilisearchDocumentSearchRequest(
    string IndexName,
    string Query,
    string Filter,
    int Page,
    int PageSize,
    string? Sort);

internal sealed record MeilisearchDocumentSearchPage(
    IReadOnlyCollection<MeilisearchSearchDocument> Documents,
    int TotalCount);
