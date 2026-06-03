using Microsoft.EntityFrameworkCore;
using Norge360.Discovery.Domain.Entities;

namespace Norge360.Discovery.Application.Abstractions;

public interface IDiscoveryDbContext
{
    DbSet<DiscoveryEvent> DiscoveryEvents { get; }
    DbSet<DiscoveryDailyAggregate> DiscoveryDailyAggregates { get; }
    DbSet<DiscoveryRanking> DiscoveryRankings { get; }
    DbSet<DiscoverySubjectSnapshot> DiscoverySubjectSnapshots { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
