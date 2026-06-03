// <copyright file="IDiscoveryEventIngestionService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.Application.Abstractions;

public interface IDiscoveryEventIngestionService
{
    Task<DiscoveryEventIngestionResponse> IngestAsync(DiscoveryEventRequest request, CancellationToken cancellationToken = default);
    Task<DiscoveryEventIngestionResponse> IngestBatchAsync(DiscoveryEventBatchRequest request, CancellationToken cancellationToken = default);
}
