// <copyright file="MessagingMessageReceipt.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingMessageReceipt
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }

    public MessagingMessage? Message { get; set; }
}
