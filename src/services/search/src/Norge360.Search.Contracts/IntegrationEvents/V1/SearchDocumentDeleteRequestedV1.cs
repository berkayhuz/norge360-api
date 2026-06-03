// <copyright file="SearchDocumentDeleteRequestedV1.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Contracts.IntegrationEvents.V1;

public sealed record SearchDocumentDeleteRequestedV1(
    Guid EventId,
    string DocumentId,
    SearchDocumentSource Source,
    string Type,
    Guid? TenantId,
    string? CorrelationId,
    string? CausationId,
    DateTime OccurredAtUtc)
{
    public const string EventName = "search.document.delete.requested";
    public const int EventVersion = 1;
    public const string RoutingKey = "search.document.delete.requested.v1";
}
