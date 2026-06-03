// <copyright file="CommunityDbContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Norge360.Community.Infrastructure.Persistence.DesignTime; public sealed class CommunityDbContextFactory : IDesignTimeDbContextFactory<CommunityDbContext> { public CommunityDbContext CreateDbContext(string[] args) { var connectionString = Environment.GetEnvironmentVariable("Norge360_COMMUNITY_MIGRATIONS_CONNECTION") ?? Environment.GetEnvironmentVariable("ConnectionStrings__CommunityConnection") ?? "Host=DB_HOST;Port=5433;Database=norge360_community;Username=DB_USER;Password=DB_PASSWORD;Include Error Detail=true"; var options = new DbContextOptionsBuilder<CommunityDbContext>().UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(CommunityDbContext).Assembly.FullName)).Options; return new CommunityDbContext(options); } }
