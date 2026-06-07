// <copyright file="SearchBootstrapHostedService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Worker.Options;
using Norge360.Messaging.RabbitMq.Connection;
using Norge360.Messaging.RabbitMq.Options;
using RabbitMQ.Client;

namespace Norge360.Accounts.Worker.Infrastructure;

public sealed class SearchBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> rabbitMqOptions,
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

        await EnsureSearchTopologyAsync(cancellationToken);

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

    private async Task EnsureSearchTopologyAsync(CancellationToken cancellationToken)
    {
        var rabbit = rabbitMqOptions.Value;
        const string searchQueueName = "norge360.search.indexer";
        var routingPatterns = new[]
        {
            "search.document.index.requested.v1",
            "search.document.delete.requested.v1",
            "search.reindex.requested.v1"
        };

        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            rabbit.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            searchQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        foreach (var routingPattern in routingPatterns)
        {
            await channel.QueueBindAsync(
                searchQueueName,
                rabbit.Exchange,
                routingPattern,
                cancellationToken: cancellationToken);
        }

        logger.LogInformation(
            "Search bootstrap ensured search topology. Exchange={Exchange} Queue={QueueName} RoutingPatterns={RoutingPatterns}",
            rabbit.Exchange,
            searchQueueName,
            string.Join(",", routingPatterns));
    }
}
