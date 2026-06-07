// <copyright file="AccountsInfrastructureDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Infrastructure.Initialization;
using Norge360.Accounts.Infrastructure.Options;
using Norge360.Accounts.Infrastructure.Persistence;
using Norge360.Accounts.Infrastructure.Repositories;
using Norge360.Accounts.Infrastructure.Services;
using Norge360.Clock;

namespace Norge360.Accounts.Infrastructure.DependencyInjection;

public static class AccountsInfrastructureDependencyInjection
{
    public static IServiceCollection AddAccountsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ReservedUsernameSeedOptions>()
            .Configure(options =>
                configuration
                    .GetSection(ReservedUsernameSeedOptions.SectionName)
                    .Bind(options));

        var connectionString = configuration.GetConnectionString("AccountsConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'AccountsConnection' is missing or empty. Set ConnectionStrings__AccountsConnection for Accounts API and Worker.");
        }

        services.AddDbContext<AccountsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAccountsUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<AccountsDbContext>());
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IReservedUsernameRepository, ReservedUsernameRepository>();
        services.AddScoped<IUsernameHistoryRepository, UsernameHistoryRepository>();
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();
        services.AddScoped<IUserFollowRepository, UserFollowRepository>();
        services.AddScoped<IProfileNotificationSubscriptionRepository, ProfileNotificationSubscriptionRepository>();
        services.AddScoped<IFollowAccessChecker, FollowAccessChecker>();
        services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
        services.AddScoped<IAccountNotificationPublisher, OutboxAccountNotificationPublisher>();
        services.AddScoped<DemoProfileSeeder>();
        services.AddScoped<IReservedUsernameInitializer, ReservedUsernameInitializer>();
        services.AddSingleton<IClock, SystemClock>();

        services.Configure<DiscoveryApiOptions>(configuration.GetSection("Services:Discovery"));
        services.AddHttpClient("discovery-accounts", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscoveryApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        if (configuration.GetValue<bool>("Services:Discovery:Enabled"))
        {
            services.AddScoped<IDiscoveryEventPublisher, HttpDiscoveryEventPublisher>();
        }
        else
        {
            services.AddScoped<IDiscoveryEventPublisher, NoOpDiscoveryEventPublisher>();
        }

        return services;
    }
}
