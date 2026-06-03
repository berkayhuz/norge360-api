// <copyright file="UpsertSearchDocumentsCommand.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Indexing.Commands;

public sealed record UpsertSearchDocumentsCommand(IReadOnlyCollection<SearchDocument> Documents) : IRequest;

public sealed class UpsertSearchDocumentsCommandHandler(ISearchIndexingService searchIndexingService)
    : IRequestHandler<UpsertSearchDocumentsCommand>
{
    public async Task Handle(UpsertSearchDocumentsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Documents);
        if (request.Documents.Count == 0)
        {
            throw new ArgumentException("At least one search document is required.", nameof(request.Documents));
        }

        if (request.Documents.Any(document => document is null))
        {
            throw new ArgumentException("Search document collection cannot contain null documents.", nameof(request.Documents));
        }

        await searchIndexingService.UpsertManyAsync(request.Documents, cancellationToken);
    }
}

