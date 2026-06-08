// <copyright file="MessagingInfrastructureDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Norge360.Clock;
using Norge360.CurrentUser;
using Norge360.Messaging.RabbitMq.DependencyInjection;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Infrastructure.Persistence;
using Norge360.MessagingService.Infrastructure.Services;
using Norge360.Persistence.EntityFrameworkCore.Auditing;

namespace Norge360.MessagingService.Infrastructure.DependencyInjection;

public static class MessagingInfrastructureDependencyInjection
{
    public static IServiceCollection AddMessagingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MessagingConnection")
            ?? throw new InvalidOperationException("Connection string 'MessagingConnection' is missing.");

        services.AddDbContext<MessagingDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(MessagingDbContext).Assembly.FullName));
            options.AddInterceptors(new AuditSaveChangesInterceptor(serviceProvider.GetRequiredService<ICurrentUserService>()));
        });

        services.AddScoped<IMessagingDbContext>(static serviceProvider => serviceProvider.GetRequiredService<MessagingDbContext>());
        services.AddScoped<IActiveConversationRegistry, DistributedActiveConversationRegistry>();
        services.AddRabbitMqMessaging(configuration);
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
