// <copyright file="SearchBootstrapHostedService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Worker.Options;

namespace Norge360.Accounts.Worker.Infrastructure;

public sealed class SearchBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SearchBootstrapOptions> options,
    ILogger<SearchBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ReindexUsersOnStartup)
        {
            logger.LogInformation("Search bootstrap skipped (SearchBootstrap:ReindexUsersOnStartup=false).");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var reindexService = scope.ServiceProvider.GetRequiredService<IUserSearchReindexService>();
        var batchSize = Math.Clamp(options.Value.BatchSize, 10, 500);
        var enqueued = await reindexService.EnqueueAllActiveUsersAsync(batchSize, cancellationToken);
        logger.LogInformation(
            "Search bootstrap queued reindex for active users. EnqueuedCount={EnqueuedCount} BatchSize={BatchSize}",
            enqueued,
            batchSize);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
