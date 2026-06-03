using Microsoft.EntityFrameworkCore;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Domain.Entities;

namespace Norge360.Discovery.Infrastructure.Persistence;

public sealed class DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options) : DbContext(options), IDiscoveryDbContext
{
    public DbSet<DiscoveryEvent> DiscoveryEvents => Set<DiscoveryEvent>();
    public DbSet<DiscoveryDailyAggregate> DiscoveryDailyAggregates => Set<DiscoveryDailyAggregate>();
    public DbSet<DiscoveryRanking> DiscoveryRankings => Set<DiscoveryRanking>();
    public DbSet<DiscoverySubjectSnapshot> DiscoverySubjectSnapshots => Set<DiscoverySubjectSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiscoveryDbContext).Assembly);
}
