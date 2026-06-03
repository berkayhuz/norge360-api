using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Infrastructure.Persistence;

namespace Norge360.Discovery.Infrastructure.DependencyInjection;

public static class DiscoveryInfrastructureDependencyInjection
{
    public static IServiceCollection AddDiscoveryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DiscoveryConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DiscoveryConnection' is missing or empty. Set ConnectionStrings__DiscoveryConnection for Discovery API and Worker.");
        }

        services.AddDbContext<DiscoveryDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDiscoveryDbContext>(sp => sp.GetRequiredService<DiscoveryDbContext>());
        return services;
    }
}
