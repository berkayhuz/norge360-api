// <copyright file="MessagingRealtimeEventNames.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.API.Hubs;

public static class MessagingRealtimeEventNames
{
    public const string MessageCreated = "messaging.message.created";
    public const string MessageUpdated = "messaging.message.updated";
    public const string ConversationUpdated = "messaging.conversation.updated";
    public const string ReadReceiptUpdated = "messaging.receipt.read";
    public const string Typing = "messaging.typing";
    public const string Presence = "messaging.presence";
}
