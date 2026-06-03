// <copyright file="UpsertSearchDocumentCommand.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Indexing.Commands;

public sealed record UpsertSearchDocumentCommand(SearchDocument Document) : IRequest;

public sealed class UpsertSearchDocumentCommandHandler(ISearchIndexingService searchIndexingService)
    : IRequestHandler<UpsertSearchDocumentCommand>
{
    public async Task Handle(UpsertSearchDocumentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Document);

        await searchIndexingService.UpsertAsync(request.Document, cancellationToken);
    }
}

