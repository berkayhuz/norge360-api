using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.Application.Abstractions;

public interface IDiscoveryEventIngestionService
{
    Task<DiscoveryEventIngestionResponse> IngestAsync(DiscoveryEventRequest request, CancellationToken cancellationToken = default);
    Task<DiscoveryEventIngestionResponse> IngestBatchAsync(DiscoveryEventBatchRequest request, CancellationToken cancellationToken = default);
}
