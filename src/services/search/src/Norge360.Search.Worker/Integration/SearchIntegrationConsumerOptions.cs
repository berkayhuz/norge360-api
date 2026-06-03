// <copyright file="SearchIntegrationConsumerOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Norge360.Search.Worker.Integration;

public sealed class SearchIntegrationConsumerOptions
{
    public const string SectionName = "Search:IntegrationConsumer";

    public bool Enabled { get; init; } = true;

    [Required]
    public string QueueName { get; init; } = "norge360.search.indexer";

    [Range(0, 120)]
    public int ProcessingFailureRequeueDelaySeconds { get; init; } = 0;

    public IReadOnlyCollection<string> RoutingKeyPatterns { get; init; } =
    [
        "search.document.index.requested.v1",
        "search.document.delete.requested.v1",
        "search.reindex.requested.v1"
    ];
}
