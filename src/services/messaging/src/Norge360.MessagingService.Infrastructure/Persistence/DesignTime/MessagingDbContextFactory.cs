// <copyright file="MessagingDbContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Norge360.MessagingService.Infrastructure.Persistence.DesignTime;

public sealed class MessagingDbContextFactory : IDesignTimeDbContextFactory<MessagingDbContext>
{
    public MessagingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Norge360_MESSAGING_MIGRATIONS_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__MessagingConnection")
            ?? "Host=DB_HOST;Port=5433;Database=norge360_messaging;Username=DB_USER;Password=DB_PASSWORD;Include Error Detail=true";

        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(MessagingDbContext).Assembly.FullName))
            .Options;

        return new MessagingDbContext(options);
    }
}
