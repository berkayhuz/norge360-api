// <copyright file="AccountsRabbitMqTopologyInitializer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Messaging.RabbitMq.Connection;
using RabbitMQ.Client;

namespace Norge360.Accounts.Worker.Integration;

public sealed class AccountsRabbitMqTopologyInitializer(
    RabbitMqConnectionProvider connectionProvider,
    IOptions<AccountsIntegrationOptions> options,
    ILogger<AccountsRabbitMqTopologyInitializer> logger) : IAccountsRabbitMqTopologyInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var value = options.Value;
        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            value.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await DeclareMainQueueAsync(channel, value, cancellationToken);
        await DeclareRetryQueueAsync(
            channel,
            value.Exchange,
            value.RetryQueue1,
            value.RetryRoutingKey1,
            value.RetryDelay1,
            value.RoutingKey,
            cancellationToken);
        await DeclareRetryQueueAsync(
            channel,
            value.Exchange,
            value.RetryQueue2,
            value.RetryRoutingKey2,
            value.RetryDelay2,
            value.RoutingKey,
            cancellationToken);
        await DeclareDeadLetterQueueAsync(channel, value, cancellationToken);

        logger.LogInformation(
            "Accounts RabbitMQ topology declared. Exchange={Exchange} MainQueue={QueueName} RoutingKey={RoutingKey} RetryQueue1={RetryQueue1} RetryQueue2={RetryQueue2} DeadLetterQueue={DeadLetterQueue}",
            value.Exchange,
            value.QueueName,
            value.RoutingKey,
            value.RetryQueue1,
            value.RetryQueue2,
            value.DeadLetterQueue);
    }

    private static async Task DeclareMainQueueAsync(
        IChannel channel,
        AccountsIntegrationOptions options,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            options.QueueName,
            options.Exchange,
            options.RoutingKey,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareRetryQueueAsync(
        IChannel channel,
        string exchange,
        string queueName,
        string retryRoutingKey,
        TimeSpan retryDelay,
        string mainRoutingKey,
        CancellationToken cancellationToken)
    {
        var queueArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = ToMessageTtlMilliseconds(retryDelay),
            ["x-dead-letter-exchange"] = exchange,
            ["x-dead-letter-routing-key"] = mainRoutingKey
        };

        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queueName,
            exchange,
            retryRoutingKey,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareDeadLetterQueueAsync(
        IChannel channel,
        AccountsIntegrationOptions options,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            options.DeadLetterQueue,
            options.Exchange,
            options.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);
    }

    private static int ToMessageTtlMilliseconds(TimeSpan value) =>
        checked((int)value.TotalMilliseconds);
}
