// <copyright file="DiscoveryDbHealthCheck.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
