// <copyright file="MessagingConversation.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingConversation
{
    public Guid Id { get; set; }
    public ConversationKind Kind { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public ConversationRequestStatus RequestStatus { get; set; } = ConversationRequestStatus.None;
    public Guid CreatedByUserId { get; set; }
    public Guid? LastMessageId { get; set; }
    public Guid? LastMessageSenderUserId { get; set; }
    public DateTimeOffset? LastMessageAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DefaultDisappearingTtlSeconds { get; set; }

    public ICollection<MessagingConversationParticipant> Participants { get; } = new List<MessagingConversationParticipant>();
    public ICollection<MessagingMessage> Messages { get; } = new List<MessagingMessage>();
}
