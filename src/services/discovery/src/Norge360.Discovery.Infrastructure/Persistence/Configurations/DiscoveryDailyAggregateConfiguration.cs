using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Norge360.Discovery.Domain.Entities;

namespace Norge360.Discovery.Infrastructure.Persistence.Configurations;

public sealed class DiscoveryDailyAggregateConfiguration : IEntityTypeConfiguration<DiscoveryDailyAggregate>
{
    public void Configure(EntityTypeBuilder<DiscoveryDailyAggregate> builder)
    {
        builder.ToTable("DiscoveryDailyAggregates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetType).HasConversion<short>();
        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.Date }).IsUnique();
        builder.HasIndex(x => x.Date);
    }
}
