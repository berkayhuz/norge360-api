using Microsoft.Extensions.DependencyInjection;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Application.Services;

namespace Norge360.Discovery.Application.DependencyInjection;

public static class DiscoveryApplicationDependencyInjection
{
    public static IServiceCollection AddDiscoveryApplication(this IServiceCollection services)
    {
        services.AddScoped<IDiscoveryEventIngestionService, DiscoveryEventIngestionService>();
        services.AddScoped<IDiscoveryRankingService, DiscoveryRankingService>();
        services.AddScoped<IDiscoverySnapshotService, DiscoverySnapshotService>();
        return services;
    }
}
