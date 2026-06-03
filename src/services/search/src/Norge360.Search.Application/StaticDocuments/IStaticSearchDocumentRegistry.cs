// <copyright file="IStaticSearchDocumentRegistry.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.StaticDocuments;

public interface IStaticSearchDocumentRegistry
{
    Task<IReadOnlyCollection<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken);
}

