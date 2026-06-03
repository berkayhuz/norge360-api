// <copyright file="AccountsDbContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Norge360.Accounts.Infrastructure.Persistence.DesignTime;

public sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Norge360_ACCOUNTS_MIGRATIONS_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__AccountsConnection")
            ?? "Host=DB_HOST;Port=5433;Database=norge360_accounts;Username=DB_USER;Password=DB_PASSWORD;Include Error Detail=true";

        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AccountsDbContext).Assembly.FullName))
            .Options;

        return new AccountsDbContext(options);
    }
}
