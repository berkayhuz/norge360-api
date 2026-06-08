// <copyright file="MessagingConversationMessageCreatedV1.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Contracts.IntegrationEvents.V1;

public sealed record MessagingConversationMessageCreatedV1(
    Guid EventId,
    Guid ConversationId,
    Guid MessageId,
    Guid SenderUserId,
    IReadOnlyCollection<Guid> RecipientUserIds,
    DateTimeOffset OccurredAtUtc)
{
    public const string EventName = "messaging.message.created";
    public const int EventVersion = 1;
    public const string RoutingKey = "messaging.message.created.v1";
}
