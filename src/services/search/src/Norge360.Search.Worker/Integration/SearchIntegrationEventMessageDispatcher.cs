// <copyright file="SearchIntegrationEventMessageDispatcher.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Contracts.IntegrationEvents.V1;

namespace Norge360.Search.Worker.Integration;

public sealed class SearchIntegrationEventMessageDispatcher(
    ISearchIntegrationEventIngestionService ingestionService,
    ILogger<SearchIntegrationEventMessageDispatcher> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<SearchIntegrationDispatchStatus> DispatchAsync(
        string routingKey,
        string payload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            switch (ResolveEventKind(routingKey))
            {
                case SearchIntegrationEventKind.Index:
                    {
                        var integrationEvent = Deserialize<SearchDocumentIndexRequestedV1>(payload, nameof(SearchDocumentIndexRequestedV1));
                        await ingestionService.HandleAsync(integrationEvent, cancellationToken);

                        logger.LogInformation(
                            "Search index integration event dispatched. RoutingKey={RoutingKey} EventId={EventId} DocumentId={DocumentId} Source={Source}",
                            routingKey,
                            integrationEvent.EventId,
                            integrationEvent.Document.Id,
                            integrationEvent.Document.Source);
                        return SearchIntegrationDispatchStatus.Dispatched;
                    }
                case SearchIntegrationEventKind.Delete:
                    {
                        var integrationEvent = Deserialize<SearchDocumentDeleteRequestedV1>(payload, nameof(SearchDocumentDeleteRequestedV1));
                        await ingestionService.HandleAsync(integrationEvent, cancellationToken);

                        logger.LogInformation(
                            "Search delete integration event dispatched. RoutingKey={RoutingKey} EventId={EventId} DocumentId={DocumentId} Source={Source}",
                            routingKey,
                            integrationEvent.EventId,
                            integrationEvent.DocumentId,
                            integrationEvent.Source);
                        return SearchIntegrationDispatchStatus.Dispatched;
                    }
                case SearchIntegrationEventKind.Reindex:
                    {
                        var integrationEvent = Deserialize<SearchReindexRequestedV1>(payload, nameof(SearchReindexRequestedV1));
                        await ingestionService.HandleAsync(integrationEvent, cancellationToken);

                        logger.LogInformation(
                            "Search reindex integration event dispatched (deferred behavior). RoutingKey={RoutingKey} EventId={EventId} Source={Source}",
                            routingKey,
                            integrationEvent.EventId,
                            integrationEvent.Source);
                        return SearchIntegrationDispatchStatus.Dispatched;
                    }
                default:
                    logger.LogWarning(
                        "Search integration event skipped due to unsupported routing key. RoutingKey={RoutingKey}",
                        routingKey);
                    return SearchIntegrationDispatchStatus.UnsupportedRoutingKey;
            }
        }
        catch (JsonException exception)
        {
            logger.LogError(
                exception,
                "Search integration event payload deserialization failed. RoutingKey={RoutingKey}",
                routingKey);
            return SearchIntegrationDispatchStatus.InvalidPayload;
        }
    }

    private static T Deserialize<T>(string payload, string eventName)
    {
        var message = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        return message ?? throw new JsonException($"{eventName} payload could not be deserialized.");
    }

    private static SearchIntegrationEventKind ResolveEventKind(string routingKey)
    {
        if (routingKey.StartsWith("search.index.", StringComparison.OrdinalIgnoreCase) ||
            routingKey.Equals(SearchDocumentIndexRequestedV1.RoutingKey, StringComparison.OrdinalIgnoreCase))
        {
            return SearchIntegrationEventKind.Index;
        }

        if (routingKey.StartsWith("search.delete.", StringComparison.OrdinalIgnoreCase) ||
            routingKey.Equals(SearchDocumentDeleteRequestedV1.RoutingKey, StringComparison.OrdinalIgnoreCase))
        {
            return SearchIntegrationEventKind.Delete;
        }

        if (routingKey.StartsWith("search.reindex.", StringComparison.OrdinalIgnoreCase) ||
            routingKey.Equals(SearchReindexRequestedV1.RoutingKey, StringComparison.OrdinalIgnoreCase))
        {
            return SearchIntegrationEventKind.Reindex;
        }

        return SearchIntegrationEventKind.Unknown;
    }

    private enum SearchIntegrationEventKind
    {
        Unknown = 0,
        Index = 1,
        Delete = 2,
        Reindex = 3
    }
}

public enum SearchIntegrationDispatchStatus
{
    Dispatched = 0,
    UnsupportedRoutingKey = 1,
    InvalidPayload = 2
}
