// <copyright file="AuthDbContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Norge360.Auth.Infrastructure.Persistence.DesignTime;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Norge360_AUTH_MIGRATIONS_CONNECTION")
            ?? Environment.GetEnvironmentVariable("IdentityConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__IdentityConnection");

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName))
            .Options;

        return new AuthDbContext(options);
    }
}
