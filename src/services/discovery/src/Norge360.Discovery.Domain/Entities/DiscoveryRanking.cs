// <copyright file="DiscoveryRanking.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Domain.Entities;

public sealed class DiscoveryRanking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DiscoveryRankingType RankingType { get; set; }
    public DiscoverySubjectType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public decimal Score { get; set; }
    public int Rank { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public DateTime ComputedAt { get; set; }
}
