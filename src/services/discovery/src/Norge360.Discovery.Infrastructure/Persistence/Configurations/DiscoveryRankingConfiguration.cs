using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Discovery.Domain.Entities;

namespace Norge360.Discovery.Infrastructure.Persistence.Configurations;

public sealed class DiscoveryRankingConfiguration : IEntityTypeConfiguration<DiscoveryRanking>
{
    public void Configure(EntityTypeBuilder<DiscoveryRanking> builder)
    {
        builder.ToTable("DiscoveryRankings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RankingType).HasConversion<short>();
        builder.Property(x => x.TargetType).HasConversion<short>();
        builder.Property(x => x.Score).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.RankingType, x.Rank });
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
        builder.HasIndex(x => x.Score);
    }
}
