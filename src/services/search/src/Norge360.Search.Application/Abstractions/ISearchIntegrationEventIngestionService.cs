// <copyright file="ISearchIntegrationEventIngestionService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.IntegrationEvents.V1;

namespace Norge360.Search.Application.Abstractions;

public interface ISearchIntegrationEventIngestionService
{
    Task HandleAsync(SearchDocumentIndexRequestedV1 integrationEvent, CancellationToken cancellationToken);

    Task HandleAsync(SearchDocumentDeleteRequestedV1 integrationEvent, CancellationToken cancellationToken);

    Task HandleAsync(SearchReindexRequestedV1 integrationEvent, CancellationToken cancellationToken);
}
