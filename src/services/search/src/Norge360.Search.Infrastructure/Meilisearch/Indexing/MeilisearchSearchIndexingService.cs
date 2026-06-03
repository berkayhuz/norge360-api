// <copyright file="MeilisearchSearchIndexingService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Infrastructure.Meilisearch.Client;
using Norge360.Search.Infrastructure.Meilisearch.Documents;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Infrastructure.Meilisearch.Indexing;

internal sealed class MeilisearchSearchIndexingService(
    IOptions<SearchOptions> searchOptions,
    IMeilisearchIndexInitializer indexInitializer,
    IMeilisearchDocumentClient documentClient,
    MeilisearchDocumentMapper mapper) : ISearchIndexingService
{
    public async Task UpsertAsync(SearchDocument document, CancellationToken cancellationToken)
    {
        EnsureProviderIsMeilisearch();
        ValidateDocument(document);

        var normalized = EnsureIndexedAtUtc(document);
        var stored = mapper.ToStoredDocument(normalized);

        await indexInitializer.EnsureInitializedAsync(cancellationToken);
        await documentClient.UpsertAsync(searchOptions.Value.IndexName, stored, cancellationToken);
    }

    public async Task UpsertManyAsync(IReadOnlyCollection<SearchDocument> documents, CancellationToken cancellationToken)
    {
        EnsureProviderIsMeilisearch();
        if (documents.Count == 0)
        {
            return;
        }

        var failures = new List<string>();
        var mapped = new List<MeilisearchSearchDocument>(documents.Count);

        foreach (var document in documents)
        {
            var validationErrors = SearchDocumentSecurityValidator.Validate(document);
            if (validationErrors.Count > 0)
            {
                failures.Add($"Document '{document.Id}': {string.Join("; ", validationErrors)}");
                continue;
            }

            mapped.Add(mapper.ToStoredDocument(EnsureIndexedAtUtc(document)));
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"Cannot index invalid documents. {string.Join(" | ", failures)}");
        }

        await indexInitializer.EnsureInitializedAsync(cancellationToken);
        await documentClient.UpsertManyAsync(searchOptions.Value.IndexName, mapped, cancellationToken);
    }

    public async Task SoftDeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        EnsureProviderIsMeilisearch();
        EnsureDocumentId(documentId);

        await indexInitializer.EnsureInitializedAsync(cancellationToken);
        var existing = await documentClient.GetDocumentAsync(searchOptions.Value.IndexName, documentId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        var deleted = existing with
        {
            IsDeleted = true,
            IndexedAtUtc = DateTimeOffset.UtcNow
        };

        await documentClient.UpsertAsync(searchOptions.Value.IndexName, deleted, cancellationToken);
    }

    public async Task HardDeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        EnsureProviderIsMeilisearch();
        EnsureDocumentId(documentId);

        await indexInitializer.EnsureInitializedAsync(cancellationToken);
        await documentClient.HardDeleteAsync(searchOptions.Value.IndexName, documentId, cancellationToken);
    }

    private void EnsureProviderIsMeilisearch()
    {
        if (!searchOptions.Value.Provider.Equals("Meilisearch", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Search provider '{searchOptions.Value.Provider}' is not supported by {nameof(MeilisearchSearchIndexingService)}.");
        }
    }

    private static void ValidateDocument(SearchDocument document)
    {
        var validationErrors = SearchDocumentSecurityValidator.Validate(document);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot index invalid document '{document.Id}'. {string.Join("; ", validationErrors)}");
        }
    }

    private static SearchDocument EnsureIndexedAtUtc(SearchDocument document)
    {
        if (document.IndexedAtUtc != default)
        {
            return document;
        }

        return document with { IndexedAtUtc = DateTimeOffset.UtcNow };
    }

    private static void EnsureDocumentId(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document id is required.", nameof(documentId));
        }
    }
}

