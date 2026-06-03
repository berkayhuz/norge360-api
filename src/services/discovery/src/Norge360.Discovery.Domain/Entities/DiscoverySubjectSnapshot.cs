// <copyright file="DiscoverySubjectSnapshot.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Domain.Entities;

public sealed class DiscoverySubjectSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DiscoverySubjectType SubjectType { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? AuthUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int FollowersCount { get; set; }
    public int PostsCount { get; set; }
    public bool IsVerified { get; set; }
    public string Visibility { get; set; } = "Public";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; }
}
