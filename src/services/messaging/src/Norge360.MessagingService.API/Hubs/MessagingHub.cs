// <copyright file="MessagingHub.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.API.Hubs;

[Authorize]
public sealed class MessagingHub(
    IMessagingService messagingService,
    IMessagingRealtimePublisher realtimePublisher,
    IActiveConversationRegistry activeConversationRegistry) : Hub
{
    private static readonly TimeSpan ActiveConversationTtl = TimeSpan.FromSeconds(45);

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            throw new HubException("authenticated_user_required");
        }

        var access = await messagingService.CanAccessConversationAsync(userId, conversationId, Context.ConnectionAborted);
        if (!access.Succeeded || !access.Value)
        {
            throw new HubException("conversation_access_denied");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId), Context.ConnectionAborted);
        await activeConversationRegistry.MarkActiveAsync(userId, conversationId, ActiveConversationTtl, Context.ConnectionAborted);
        await PublishPresenceAsync(userId, conversationId, isOnline: true);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        var userId = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId), Context.ConnectionAborted);
        if (userId != Guid.Empty)
        {
            await activeConversationRegistry.ClearActiveAsync(userId, conversationId, Context.ConnectionAborted);
            await realtimePublisher.PresenceUpdatedAsync(conversationId, userId, isOnline: false, Context.ConnectionAborted);
        }
    }

    public async Task SetActiveConversation(Guid conversationId, bool active)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            throw new HubException("authenticated_user_required");
        }

        if (active)
        {
            var access = await messagingService.CanAccessConversationAsync(userId, conversationId, Context.ConnectionAborted);
            if (!access.Succeeded || !access.Value)
            {
                throw new HubException("conversation_access_denied");
            }

            await activeConversationRegistry.MarkActiveAsync(userId, conversationId, ActiveConversationTtl, Context.ConnectionAborted);
            await PublishPresenceAsync(userId, conversationId, isOnline: true);
        }
        else
        {
            await activeConversationRegistry.ClearActiveAsync(userId, conversationId, Context.ConnectionAborted);
            await realtimePublisher.PresenceUpdatedAsync(conversationId, userId, isOnline: false, Context.ConnectionAborted);
        }
    }

    public async Task Typing(Guid conversationId, bool isTyping)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            throw new HubException("authenticated_user_required");
        }

        var access = await messagingService.CanAccessConversationAsync(userId, conversationId, Context.ConnectionAborted);
        if (!access.Succeeded || !access.Value)
        {
            throw new HubException("conversation_access_denied");
        }

        var settings = await messagingService.GetSettingsAsync(userId, Context.ConnectionAborted);
        if (settings.Value?.TypingIndicatorsEnabled != false)
        {
            await realtimePublisher.TypingAsync(conversationId, userId, isTyping, Context.ConnectionAborted);
        }

        await activeConversationRegistry.MarkActiveAsync(userId, conversationId, ActiveConversationTtl, Context.ConnectionAborted);
        await PublishPresenceAsync(userId, conversationId, isOnline: true);
    }

    internal static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId:D}";
    internal static string UserGroup(Guid userId) => $"user:{userId:D}";

    private async Task PublishPresenceAsync(Guid userId, Guid conversationId, bool isOnline)
    {
        var settings = await messagingService.GetSettingsAsync(userId, Context.ConnectionAborted);
        if (isOnline && settings.Value?.OnlineVisibility == MessagingOnlineVisibility.Nobody)
        {
            return;
        }

        await realtimePublisher.PresenceUpdatedAsync(conversationId, userId, isOnline, Context.ConnectionAborted);
    }

    private Guid GetUserId()
    {
        var subject = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                      Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
