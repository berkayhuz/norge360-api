// <copyright file="SearchDocumentIndexRequestedV1.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Contracts.IntegrationEvents.V1;

public sealed record SearchDocumentIndexRequestedV1(
    Guid EventId,
    SearchDocument Document,
    string? CorrelationId,
    string? CausationId,
    DateTime OccurredAtUtc)
{
    public const string EventName = "search.document.index.requested";
    public const int EventVersion = 1;
    public const string RoutingKey = "search.document.index.requested.v1";
}
