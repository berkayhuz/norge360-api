// <copyright file="MessagingDbContext.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Domain.Entities;

namespace Norge360.MessagingService.Infrastructure.Persistence;

public sealed class MessagingDbContext(DbContextOptions<MessagingDbContext> options)
    : DbContext(options), IMessagingDbContext
{
    public DbSet<MessagingConversation> Conversations => Set<MessagingConversation>();
    public DbSet<MessagingConversationParticipant> ConversationParticipants => Set<MessagingConversationParticipant>();
    public DbSet<MessagingMessage> Messages => Set<MessagingMessage>();
    public DbSet<MessagingMessageAttachment> MessageAttachments => Set<MessagingMessageAttachment>();
    public DbSet<MessagingMessageReaction> MessageReactions => Set<MessagingMessageReaction>();
    public DbSet<MessagingMessageReceipt> MessageReceipts => Set<MessagingMessageReceipt>();
    public DbSet<MessagingUserSettings> UserSettings => Set<MessagingUserSettings>();
    public DbSet<MessagingUserDevice> UserDevices => Set<MessagingUserDevice>();
    public DbSet<MessagingConversationReport> Reports => Set<MessagingConversationReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagingDbContext).Assembly);
    }
}
