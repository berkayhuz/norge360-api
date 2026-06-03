using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.API.Services;

public interface IAccountsDiscoveryBackfillService
{
    Task<DiscoveryBackfillResponse> BackfillAsync(int take, int maxBatches, CancellationToken cancellationToken = default);
}
