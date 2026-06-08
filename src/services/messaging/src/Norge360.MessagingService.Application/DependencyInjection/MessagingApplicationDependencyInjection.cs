// <copyright file="MessagingApplicationDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Application.Options;
using Norge360.MessagingService.Application.Services;

namespace Norge360.MessagingService.Application.DependencyInjection;

public static class MessagingApplicationDependencyInjection
{
    public static IServiceCollection AddMessagingApplication(this IServiceCollection services)
    {
        services
            .AddOptions<MessagingRulesOptions>()
            .Validate(options => options.EditWindowSeconds is > 0 and <= 3600, "Messaging edit window must be between 1 second and 1 hour.")
            .Validate(options => options.RecallWindowSeconds is > 0 and <= 3600, "Messaging recall window must be between 1 second and 1 hour.")
            .Validate(options => options.MaxPageSize is >= 1 and <= 250, "Messaging max page size must be between 1 and 250.")
            .Validate(options => options.MaxGroupParticipants is >= 2 and <= 1000, "Messaging max group participants must be between 2 and 1000.")
            .Validate(options => options.MaxBulkRecipients is >= 1 and <= 500, "Messaging max bulk recipients must be between 1 and 500.")
            .ValidateOnStart();

        services.AddScoped<IMessagingService, Services.MessagingService>();
        services.TryAddScoped<IMessagingRealtimePublisher, NoOpMessagingRealtimePublisher>();
        services.TryAddScoped<IActiveConversationRegistry, NoOpActiveConversationRegistry>();
        services.TryAddScoped<IUserRelationshipReader, AllowAllRelationshipReader>();
        return services;
    }
}
