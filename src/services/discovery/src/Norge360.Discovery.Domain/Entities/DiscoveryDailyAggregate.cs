// <copyright file="DiscoveryDailyAggregate.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Domain.Entities;

public sealed class DiscoveryDailyAggregate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DiscoverySubjectType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public DateOnly Date { get; set; }
    public int FollowPoints { get; set; }
    public int ProfileViewPoints { get; set; }
    public int LikePoints { get; set; }
    public int CommentPoints { get; set; }
    public int NegativePoints { get; set; }
    public int RawScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
