using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.Application.Abstractions;

public interface IDiscoverySnapshotService
{
    Task<DiscoverySnapshotBatchUpsertResponse> UpsertBatchAsync(
        DiscoverySnapshotBatchRequest request,
        CancellationToken cancellationToken = default);
}
