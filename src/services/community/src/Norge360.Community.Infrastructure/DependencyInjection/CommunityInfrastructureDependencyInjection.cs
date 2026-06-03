// <copyright file="CommunityInfrastructureDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Norge360.Clock;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Infrastructure.Initialization;
using Norge360.Community.Infrastructure.Options;
using Norge360.Community.Infrastructure.Persistence;
using Norge360.Community.Infrastructure.Services;

namespace Norge360.Community.Infrastructure.DependencyInjection;

public static class CommunityInfrastructureDependencyInjection
{
    public static IServiceCollection AddCommunityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CommunityConnection") ?? throw new InvalidOperationException("Connection string 'CommunityConnection' is missing.");

        services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICommunityUnitOfWork>(sp => sp.GetRequiredService<CommunityDbContext>());
        services.AddScoped<ICommunityDbContext>(sp => sp.GetRequiredService<CommunityDbContext>());

        services.Configure<AccountsApiOptions>(configuration.GetSection("Services:Accounts"));
        services.Configure<DiscoveryApiOptions>(configuration.GetSection("Services:Discovery"));
        services.AddOptions<InternalServiceSigningOptions>()
            .Bind(configuration.GetSection("InternalServices:Signing"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<InternalServiceSigningOptions>, InternalServiceSigningOptionsValidation>();
        services.AddHttpClient("accounts-community", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AccountsApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient("discovery-community", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscoveryApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<ICommunityAuthorProfileProvider, AccountsCommunityAuthorProfileProvider>();
        services.AddScoped<ICommunityVisibilityService, CommunityVisibilityService>();
        services.AddScoped<DemoCommunitySeeder>();
        var signingEnabled = configuration.GetValue<bool>("InternalServices:Signing:Enabled");
        if (signingEnabled)
        {
            services.AddSingleton<IInternalServiceRequestSigner, HmacInternalServiceRequestSigner>();
        }
        else
        {
            services.AddSingleton<IInternalServiceRequestSigner, NoOpInternalServiceRequestSigner>();
        }

        if (configuration.GetValue<bool>("Services:Discovery:Enabled"))
        {
            services.AddScoped<IDiscoveryEventPublisher, HttpDiscoveryEventPublisher>();
        }
        else
        {
            services.AddScoped<IDiscoveryEventPublisher, NoOpDiscoveryEventPublisher>();
        }

        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}

