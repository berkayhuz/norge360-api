// <copyright file="IntegrationEventOutbox.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Services;

public sealed class IntegrationEventOutbox(AccountsDbContext dbContext) : IIntegrationEventOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync<TEvent>(
        Guid eventId,
        string eventName,
        int eventVersion,
        string routingKey,
        string source,
        TEvent payload,
        string? correlationId,
        string? traceId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessage
        {
            EventId = eventId,
            EventName = eventName,
            EventVersion = eventVersion,
            Source = source,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(payload, SerializerOptions),
            CorrelationId = correlationId,
            TraceId = traceId,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = DateTime.UtcNow
        };

        await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
