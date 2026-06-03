using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Norge360.Discovery.Infrastructure.Persistence;

namespace Norge360.Discovery.Infrastructure.HealthChecks;

public sealed class DiscoveryDbHealthCheck(IServiceScopeFactory serviceScopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DiscoveryDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Discovery database is reachable.")
                : HealthCheckResult.Unhealthy("Discovery database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Discovery database health check failed.", ex);
        }
    }
}
