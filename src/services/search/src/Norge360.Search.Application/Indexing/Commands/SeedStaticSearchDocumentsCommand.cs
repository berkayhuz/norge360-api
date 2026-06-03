// <copyright file="SeedStaticSearchDocumentsCommand.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Norge360.Search.Application.StaticDocuments;

namespace Norge360.Search.Application.Indexing.Commands;

public sealed record SeedStaticSearchDocumentsCommand : IRequest<int>;

public sealed class SeedStaticSearchDocumentsCommandHandler(
    IStaticSearchDocumentRegistry staticSearchDocumentRegistry,
    ISender sender) : IRequestHandler<SeedStaticSearchDocumentsCommand, int>
{
    public async Task<int> Handle(SeedStaticSearchDocumentsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documents = await staticSearchDocumentRegistry.GetDocumentsAsync(cancellationToken);
        if (documents.Count == 0)
        {
            return 0;
        }

        await sender.Send(new UpsertSearchDocumentsCommand(documents), cancellationToken);
        return documents.Count;
    }
}

