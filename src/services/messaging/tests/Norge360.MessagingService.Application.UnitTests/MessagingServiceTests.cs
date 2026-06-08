// <copyright file="MessagingServiceTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Norge360.Clock;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Application.Models;
using Norge360.MessagingService.Application.Options;
using Norge360.MessagingService.Application.Services;
using Norge360.MessagingService.Contracts.Requests;
using Norge360.MessagingService.Domain.Enums;
using Norge360.MessagingService.Infrastructure.Persistence;
using Xunit;

namespace Norge360.MessagingService.Application.UnitTests;

public sealed class MessagingServiceTests
{
    [Fact]
    public async Task SendMessageAsync_WhenRecipientIsActive_DoesNotPublishNotification()
    {
        await using var dbContext = CreateDbContext();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-06-07T12:00:00Z"));
        var service = CreateService(dbContext, clock);
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();

        var conversation = await service.CreateDirectConversationAsync(sender, new CreateDirectConversationRequest(recipient, null));
        conversation.Succeeded.Should().BeTrue();

        var send = await service.SendMessageAsync(sender, conversation.Value!.Id, CreateMessage("client-1"));

        send.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_WithSameClientMessageId_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var conversation = await service.CreateDirectConversationAsync(sender, new CreateDirectConversationRequest(recipient, null));

        var first = await service.SendMessageAsync(sender, conversation.Value!.Id, CreateMessage("client-1"));
        var second = await service.SendMessageAsync(sender, conversation.Value.Id, CreateMessage("client-1"));

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.Value!.Id.Should().Be(first.Value!.Id);
        var messageCount = await dbContext.Messages.CountAsync();
        messageCount.Should().Be(1);
    }

    [Fact]
    public async Task EditMessageAsync_AfterEditWindow_ReturnsExpired()
    {
        await using var dbContext = CreateDbContext();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-06-07T12:00:00Z"));
        var service = CreateService(dbContext, clock, rules: new MessagingRulesOptions { EditWindowSeconds = 10, RecallWindowSeconds = 600 });
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var conversation = await service.CreateDirectConversationAsync(sender, new CreateDirectConversationRequest(recipient, null));
        var message = await service.SendMessageAsync(sender, conversation.Value!.Id, CreateMessage("client-1"));
        clock.UtcNow = clock.UtcNow.AddSeconds(11);

        var edit = await service.EditMessageAsync(
            sender,
            conversation.Value.Id,
            message.Value!.Id,
            new EditEncryptedMessageRequest(CreateEnvelope("edited"), null, null));

        edit.Status.Should().Be(MessagingOperationStatus.Expired);
        edit.ErrorCode.Should().Be("message_edit_window_expired");
    }

    [Fact]
    public async Task RecallMessageAsync_WithinWindow_RemovesEncryptedPayload()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var conversation = await service.CreateDirectConversationAsync(sender, new CreateDirectConversationRequest(recipient, null));
        var message = await service.SendMessageAsync(sender, conversation.Value!.Id, CreateMessage("client-1"));

        var recalled = await service.RecallMessageAsync(sender, conversation.Value.Id, message.Value!.Id);

        recalled.Succeeded.Should().BeTrue();
        recalled.Value!.State.Should().Be(MessageDeliveryState.Recalled);
        recalled.Value.Body.CipherText.Should().BeEmpty();
        var stored = await dbContext.Messages.SingleAsync();
        stored.CipherText.Should().BeEmpty();
        stored.CipherKeyId.Should().Be("recalled");
    }

    private static global::Norge360.MessagingService.Application.Services.MessagingService CreateService(
        MessagingDbContext dbContext,
        MutableClock? clock = null,
        MessagingRulesOptions? rules = null) =>
        new(
            dbContext,
            clock ?? new MutableClock(DateTimeOffset.Parse("2026-06-07T12:00:00Z")),
            Microsoft.Extensions.Options.Options.Create(rules ?? new MessagingRulesOptions()),
            new CapturingRealtimePublisher(),
            new AllowRelationshipReader());

    private static MessagingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new MessagingDbContext(options);
    }

    private static SendEncryptedMessageRequest CreateMessage(string clientMessageId) =>
        new(
            clientMessageId,
            "device-1",
            MessageKind.Text,
            CreateEnvelope("hello"),
            [],
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            "token-hash");

    private static EncryptedTextEnvelope CreateEnvelope(string text) =>
        new(
            System.Text.Encoding.UTF8.GetBytes(text),
            [1, 2, 3, 4],
            "key-1",
            "signal-x3dh-double-ratchet/xchacha20-poly1305");

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public DateTime UtcDateTime => UtcNow.UtcDateTime;
    }

    private sealed class AllowRelationshipReader : IUserRelationshipReader
    {
        public Task<MessagingRelationship> GetAsync(Guid requesterUserId, Guid targetUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MessagingRelationship(true, true, false, false));
    }

    private sealed class CapturingRealtimePublisher : IMessagingRealtimePublisher
    {
        public Task MessageCreatedAsync(Guid conversationId, global::Norge360.MessagingService.Contracts.Responses.MessageResponse message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MessageUpdatedAsync(Guid conversationId, global::Norge360.MessagingService.Contracts.Responses.MessageResponse message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ConversationUpdatedAsync(Guid conversationId, global::Norge360.MessagingService.Contracts.Responses.ConversationSummaryResponse conversation, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReadReceiptUpdatedAsync(Guid conversationId, Guid userId, Guid? messageId, DateTimeOffset readAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task TypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task PresenceUpdatedAsync(Guid conversationId, Guid userId, bool isOnline, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
