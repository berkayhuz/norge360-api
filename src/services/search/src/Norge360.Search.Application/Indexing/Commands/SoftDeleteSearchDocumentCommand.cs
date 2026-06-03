// <copyright file="SoftDeleteSearchDocumentCommand.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Norge360.Search.Application.Abstractions;

namespace Norge360.Search.Application.Indexing.Commands;

public sealed record SoftDeleteSearchDocumentCommand(string DocumentId) : IRequest;

public sealed class SoftDeleteSearchDocumentCommandHandler(ISearchIndexingService searchIndexingService)
    : IRequestHandler<SoftDeleteSearchDocumentCommand>
{
    public async Task Handle(SoftDeleteSearchDocumentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.DocumentId))
        {
            throw new ArgumentException("Document id is required.", nameof(request.DocumentId));
        }

        await searchIndexingService.SoftDeleteAsync(request.DocumentId, cancellationToken);
    }
}

