// <copyright file="UserRegisteredConsumerService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Messaging.RabbitMq.Connection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Norge360.Accounts.Worker.Integration;

public sealed class UserRegisteredConsumerService : BackgroundService
{
    private const string RetryAttemptHeader = "x-norge360-retry-attempt";
    private const string FailureReasonHeader = "x-norge360-failure-reason";
    private const string OriginalRoutingKeyHeader = "x-norge360-original-routing-key";
    private const string FailedAtUtcHeader = "x-norge360-failed-at-utc";

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAccountsRabbitMqTopologyInitializer _topologyInitializer;
    private readonly ILogger<UserRegisteredConsumerService> _logger;
    private readonly IOptionsMonitor<AccountsIntegrationOptions> _options;

    public UserRegisteredConsumerService(
        RabbitMqConnectionProvider connectionProvider,
        IServiceScopeFactory scopeFactory,
        IAccountsRabbitMqTopologyInitializer topologyInitializer,
        ILogger<UserRegisteredConsumerService> logger,
        IOptionsMonitor<AccountsIntegrationOptions> options)
    {
        _connectionProvider = connectionProvider;
        _scopeFactory = scopeFactory;
        _topologyInitializer = topologyInitializer;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.CurrentValue;

        _logger.LogInformation(
            "Accounts user registered consumer skeleton configured. Exchange={Exchange} Queue={QueueName} RoutingKey={RoutingKey} RetryQueue1={RetryQueue1} RetryQueue2={RetryQueue2} DeadLetterQueue={DeadLetterQueue} RetryRoutingKey1={RetryRoutingKey1} RetryRoutingKey2={RetryRoutingKey2} DeadLetterRoutingKey={DeadLetterRoutingKey} PrefetchCount={PrefetchCount} ConsumerTag={ConsumerTag}",
            options.Exchange,
            options.QueueName,
            options.RoutingKey,
            options.RetryQueue1,
            options.RetryQueue2,
            options.DeadLetterQueue,
            options.RetryRoutingKey1,
            options.RetryRoutingKey2,
            options.DeadLetterRoutingKey,
            options.PrefetchCount,
            options.ConsumerTag);

        await _topologyInitializer.InitializeAsync(stoppingToken);

        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(channelOptions, stoppingToken);

        await channel.BasicQosAsync(0, options.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleDeliveryAsync(channel, eventArgs, stoppingToken);
        };

        var consumerTag = await channel.BasicConsumeAsync(
            options.QueueName,
            autoAck: false,
            consumerTag: options.ConsumerTag,
            consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Accounts user registered consumer started. Queue={QueueName} ConsumerTag={ConsumerTag}",
            options.QueueName,
            consumerTag);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Accounts user registered consumer is stopping.");
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IUserRegisteredMessageProcessor>();
            var result = await processor.ProcessAsync(
                eventArgs.Body,
                CreateMetadata(eventArgs),
                stoppingToken);

            await HandleProcessingResultAsync(channel, eventArgs, result, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Accounts user registered delivery handling failed before ACK. RoutingKey={RoutingKey} MessageId={MessageId}",
                eventArgs.RoutingKey,
                eventArgs.BasicProperties.MessageId);
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
        }
    }

    private async Task HandleProcessingResultAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        UserRegisteredProcessingResult result,
        CancellationToken cancellationToken)
    {
        switch (result.Status)
        {
            case UserRegisteredProcessingStatus.Success:
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
                _logger.LogInformation(
                    "Accounts user registered message ACKed. Status={Status} Reason={Reason} UserId={UserId} MessageId={MessageId}",
                    result.Status,
                    result.Reason,
                    result.UserId,
                    eventArgs.BasicProperties.MessageId);
                return;

            case UserRegisteredProcessingStatus.PermanentFailure:
                await PublishFailureAsync(
                    channel,
                    eventArgs,
                    _options.CurrentValue.DeadLetterRoutingKey,
                    result,
                    GetRetryAttempt(eventArgs.BasicProperties.Headers),
                    cancellationToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
                return;

            case UserRegisteredProcessingStatus.TransientFailure:
                var currentAttempt = GetRetryAttempt(eventArgs.BasicProperties.Headers);
                var nextRoutingKey = GetNextFailureRoutingKey(currentAttempt);
                var nextAttempt = currentAttempt >= _options.CurrentValue.MaxRetryAttempts
                    ? currentAttempt
                    : currentAttempt + 1;

                await PublishFailureAsync(
                    channel,
                    eventArgs,
                    nextRoutingKey,
                    result,
                    nextAttempt,
                    cancellationToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
                return;

            default:
                throw new InvalidOperationException($"Unsupported processing status '{result.Status}'.");
        }
    }

    private async Task PublishFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        string routingKey,
        UserRegisteredProcessingResult result,
        int retryAttempt,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var properties = CopyProperties(eventArgs.BasicProperties);
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[RetryAttemptHeader] = retryAttempt;
        properties.Headers[FailureReasonHeader] = result.Reason;
        properties.Headers[OriginalRoutingKeyHeader] = eventArgs.RoutingKey;
        properties.Headers[FailedAtUtcHeader] = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            await channel.BasicPublishAsync(
                options.Exchange,
                routingKey,
                mandatory: true,
                basicProperties: properties,
                body: eventArgs.Body,
                cancellationToken);
        }
        catch (PublishReturnException exception)
        {
            _logger.LogError(
                exception,
                "Accounts user registered failure republish was returned by RabbitMQ. Exchange={Exchange} RoutingKey={RoutingKey} ReplyCode={ReplyCode} ReplyText={ReplyText} OriginalMessageId={MessageId}",
                exception.Exchange,
                exception.RoutingKey,
                exception.ReplyCode,
                exception.ReplyText,
                eventArgs.BasicProperties.MessageId);
            throw;
        }
        catch (PublishException exception)
        {
            _logger.LogError(
                exception,
                "Accounts user registered failure republish was not confirmed by RabbitMQ. IsReturn={IsReturn} Exchange={Exchange} RoutingKey={RoutingKey} OriginalMessageId={MessageId}",
                exception.IsReturn,
                options.Exchange,
                routingKey,
                eventArgs.BasicProperties.MessageId);
            throw;
        }

        _logger.LogWarning(
            "Accounts user registered message republished after failure. Status={Status} Reason={Reason} Exchange={Exchange} RoutingKey={RoutingKey} RetryAttempt={RetryAttempt} MessageId={MessageId}",
            result.Status,
            result.Reason,
            options.Exchange,
            routingKey,
            retryAttempt,
            eventArgs.BasicProperties.MessageId);
    }

    private string GetNextFailureRoutingKey(int currentAttempt)
    {
        var options = _options.CurrentValue;
        if (currentAttempt <= 0)
        {
            return options.RetryRoutingKey1;
        }

        if (currentAttempt == 1 && options.MaxRetryAttempts >= 2)
        {
            return options.RetryRoutingKey2;
        }

        return options.DeadLetterRoutingKey;
    }

    private static UserRegisteredMessageMetadata CreateMetadata(BasicDeliverEventArgs eventArgs) =>
        new(
            eventArgs.RoutingKey,
            eventArgs.BasicProperties.MessageId,
            eventArgs.BasicProperties.CorrelationId,
            eventArgs.BasicProperties.Type,
            TryGetIntHeader(eventArgs.BasicProperties.Headers, "event_version"),
            CopyHeaders(eventArgs.BasicProperties.Headers),
            eventArgs.Redelivered);

    private static BasicProperties CopyProperties(IReadOnlyBasicProperties source) =>
        new()
        {
            AppId = source.AppId,
            ContentEncoding = source.ContentEncoding,
            ContentType = source.ContentType,
            CorrelationId = source.CorrelationId,
            DeliveryMode = source.DeliveryMode,
            Expiration = source.Expiration,
            Headers = CopyHeaders(source.Headers),
            MessageId = source.MessageId,
            Persistent = source.Persistent,
            Priority = source.Priority,
            ReplyTo = source.ReplyTo,
            Timestamp = source.Timestamp,
            Type = source.Type,
            UserId = source.UserId
        };

    private static Dictionary<string, object?> CopyHeaders(IDictionary<string, object?>? headers) =>
        headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(headers, StringComparer.Ordinal);

    private static int GetRetryAttempt(IDictionary<string, object?>? headers) =>
        TryGetIntHeader(headers, RetryAttemptHeader) ?? 0;

    private static int? TryGetIntHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue && longValue >= int.MinValue => (int)longValue,
            byte[] bytes => ParseHeaderBytes(bytes),
            ReadOnlyMemory<byte> bytes => ParseHeaderBytes(bytes.Span),
            string stringValue when int.TryParse(stringValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var result) => result,
            _ => null
        };
    }

    private static int? ParseHeaderBytes(ReadOnlySpan<byte> bytes)
    {
        var value = System.Text.Encoding.UTF8.GetString(bytes);
        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
