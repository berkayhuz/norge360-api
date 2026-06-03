using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Norge360.Discovery.Infrastructure.Persistence.DesignTime;

public sealed class DiscoveryDbContextFactory : IDesignTimeDbContextFactory<DiscoveryDbContext>
{
    public DiscoveryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DiscoveryConnection")
            ?? "Host=DB_HOST;Port=5433;Database=norge360_discovery;Username=DB_USER;Password=DB_PASSWORD;Include Error Detail=true";

        var options = new DbContextOptionsBuilder<DiscoveryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DiscoveryDbContext(options);
    }
}
