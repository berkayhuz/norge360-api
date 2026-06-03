// <copyright file="SearchIntegrationEventConsumer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text;
using Microsoft.Extensions.Options;
using Norge360.Messaging.RabbitMq.Connection;
using Norge360.Messaging.RabbitMq.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Norge360.Search.Worker.Integration;

public sealed class SearchIntegrationEventConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<SearchIntegrationConsumerOptions> consumerOptions,
    ILogger<SearchIntegrationEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = consumerOptions.Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Search integration event consumer is disabled.");
            return;
        }

        var rabbit = rabbitMqOptions.Value;
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            rabbit.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        foreach (var routingPattern in options.RoutingKeyPatterns)
        {
            await channel.QueueBindAsync(
                options.QueueName,
                rabbit.Exchange,
                routingPattern,
                cancellationToken: stoppingToken);
        }

        await channel.BasicQosAsync(0, rabbit.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(eventArgs.Body.Span);
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<SearchIntegrationEventMessageDispatcher>();
                var dispatchStatus = await dispatcher.DispatchAsync(eventArgs.RoutingKey, payload, stoppingToken);

                switch (dispatchStatus)
                {
                    case SearchIntegrationDispatchStatus.Dispatched:
                    case SearchIntegrationDispatchStatus.UnsupportedRoutingKey:
                        await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        break;
                    case SearchIntegrationDispatchStatus.InvalidPayload:
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Search integration event processing failed. RoutingKey={RoutingKey} MessageId={MessageId}",
                    eventArgs.RoutingKey,
                    eventArgs.BasicProperties.MessageId);

                if (options.ProcessingFailureRequeueDelaySeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(options.ProcessingFailureRequeueDelaySeconds), stoppingToken);
                }

                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(options.QueueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation(
            "Search integration consumer started. Exchange={Exchange} Queue={QueueName} RoutingPatterns={RoutingPatterns}",
            rabbit.Exchange,
            options.QueueName,
            string.Join(",", options.RoutingKeyPatterns));

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
