// <copyright file="DiscoveryRankingHostedService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Worker.Options;

namespace Norge360.Discovery.Worker.HostedServices;

public sealed class DiscoveryRankingHostedService(
    IServiceProvider serviceProvider,
    IOptions<DiscoveryRankingWorkerOptions> options,
    ILogger<DiscoveryRankingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Discovery ranking worker is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.IntervalSeconds, 60, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var rankingService = scope.ServiceProvider.GetRequiredService<IDiscoveryRankingService>();
                await rankingService.RecomputeAsync(stoppingToken);
                logger.LogInformation("Discovery rankings recomputed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Discovery ranking recompute failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
