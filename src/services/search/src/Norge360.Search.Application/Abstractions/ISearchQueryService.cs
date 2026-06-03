// <copyright file="ISearchQueryService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Application.Queries;
using Norge360.Search.Application.Security;

namespace Norge360.Search.Application.Abstractions;

public interface ISearchQueryService
{
    Task<SearchResponse> SearchAsync(
        SearchRequest request,
        SearchAccessContext accessContext,
        CancellationToken cancellationToken);
}
