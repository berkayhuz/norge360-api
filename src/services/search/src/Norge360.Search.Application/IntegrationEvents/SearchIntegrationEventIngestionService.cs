// <copyright file="SearchIntegrationEventIngestionService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Microsoft.Extensions.Logging;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Application.Indexing.Commands;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Contracts.IntegrationEvents.V1;

namespace Norge360.Search.Application.IntegrationEvents;

public sealed class SearchIntegrationEventIngestionService(
    ISender sender,
    ILogger<SearchIntegrationEventIngestionService> logger) : ISearchIntegrationEventIngestionService
{
    public async Task HandleAsync(SearchDocumentIndexRequestedV1 integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(integrationEvent.Document);

        var document = NormalizeAndValidateDocument(integrationEvent.Document);

        logger.LogInformation(
            "Search index integration event received. EventId={EventId} DocumentId={DocumentId} Source={Source} Type={Type}",
            integrationEvent.EventId,
            document.Id,
            document.Source,
            document.Type);

        await sender.Send(new UpsertSearchDocumentCommand(document), cancellationToken);
    }

    public async Task HandleAsync(SearchDocumentDeleteRequestedV1 integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (string.IsNullOrWhiteSpace(integrationEvent.DocumentId))
        {
            throw new ArgumentException("Document id is required.", nameof(integrationEvent.DocumentId));
        }

        logger.LogInformation(
            "Search delete integration event received. EventId={EventId} DocumentId={DocumentId} Source={Source} Type={Type}. DeleteMode=Soft",
            integrationEvent.EventId,
            integrationEvent.DocumentId,
            integrationEvent.Source,
            integrationEvent.Type);

        await sender.Send(new SoftDeleteSearchDocumentCommand(integrationEvent.DocumentId), cancellationToken);
    }

    public Task HandleAsync(SearchReindexRequestedV1 integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        logger.LogInformation(
            "Search reindex integration event received but deferred. EventId={EventId} Source={Source} TenantId={TenantId}",
            integrationEvent.EventId,
            integrationEvent.Source,
            integrationEvent.TenantId);

        return Task.CompletedTask;
    }

    private static SearchDocument NormalizeAndValidateDocument(SearchDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
        {
            throw new ArgumentException("Document id is required.", nameof(document.Id));
        }

        if (string.IsNullOrWhiteSpace(document.Title))
        {
            throw new ArgumentException("Document title is required.", nameof(document.Title));
        }

        if (string.IsNullOrWhiteSpace(document.Type))
        {
            throw new ArgumentException("Document type is required.", nameof(document.Type));
        }

        if (string.IsNullOrWhiteSpace(document.Url))
        {
            throw new ArgumentException("Document url is required.", nameof(document.Url));
        }

        if (!Enum.IsDefined(document.Source))
        {
            throw new ArgumentOutOfRangeException(nameof(document.Source), "Document source is invalid.");
        }

        if (!Enum.IsDefined(document.Visibility))
        {
            throw new ArgumentOutOfRangeException(nameof(document.Visibility), "Document visibility is invalid.");
        }

        if (!Enum.IsDefined(document.PermissionMatchMode))
        {
            throw new ArgumentOutOfRangeException(nameof(document.PermissionMatchMode), "Permission match mode is invalid.");
        }

        var normalizedDocument = document with
        {
            RequiredPermissions = NormalizeStringCollection(document.RequiredPermissions),
            Tags = NormalizeStringCollection(document.Tags),
            AssignedUserIds = NormalizeGuidCollection(document.AssignedUserIds),
            Metadata = NormalizeMetadata(document.Metadata),
            Locale = string.IsNullOrWhiteSpace(document.Locale) ? "en" : document.Locale
        };

        var validationErrors = SearchDocumentSecurityValidator.Validate(normalizedDocument);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException($"Search document failed security validation: {string.Join("; ", validationErrors)}");
        }

        return normalizedDocument;
    }

    private static IReadOnlyCollection<string> NormalizeStringCollection(IReadOnlyCollection<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

    private static IReadOnlyCollection<Guid> NormalizeGuidCollection(IReadOnlyCollection<Guid>? values) =>
        values?
            .Where(value => value != Guid.Empty)
            .Distinct()
            .ToArray() ?? [];

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            normalized[pair.Key.Trim()] = pair.Value;
        }

        return normalized;
    }
}
