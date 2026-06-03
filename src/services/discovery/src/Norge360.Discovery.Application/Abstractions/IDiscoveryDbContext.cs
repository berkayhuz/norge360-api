// <copyright file="IDiscoveryDbContext.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

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
