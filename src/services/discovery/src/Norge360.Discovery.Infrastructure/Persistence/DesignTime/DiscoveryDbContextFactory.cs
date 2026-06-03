// <copyright file="DiscoveryDbContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

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
