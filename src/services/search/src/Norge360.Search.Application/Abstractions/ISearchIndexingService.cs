// <copyright file="ISearchIndexingService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Abstractions;

public interface ISearchIndexingService
{
    Task UpsertAsync(SearchDocument document, CancellationToken cancellationToken);

    Task UpsertManyAsync(IReadOnlyCollection<SearchDocument> documents, CancellationToken cancellationToken);

    Task SoftDeleteAsync(string documentId, CancellationToken cancellationToken);

    Task HardDeleteAsync(string documentId, CancellationToken cancellationToken);
}

