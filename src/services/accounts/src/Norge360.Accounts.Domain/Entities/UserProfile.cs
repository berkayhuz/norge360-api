// <copyright file="UserProfile.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Enums;
using Norge360.Entities;

namespace Norge360.Accounts.Domain.Entities;

public sealed class UserProfile : AuditableEntity
{
    public Guid AuthUserId { get; set; }
    public string Username { get; set; } = null!;
    public string NormalizedUsername { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarStorageKey { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public string? CoverPhotoStorageKey { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Occupation { get; set; }
    public string? Company { get; set; }
    public string? Website { get; set; }
    public int FollowersCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;
    public int PostsCount { get; set; } = 0;
    public bool IsVerified { get; set; } = false;
    public AccountType AccountType { get; set; } = Enums.AccountType.Personal;
    public ProfileVisibility ProfileVisibility { get; set; } = Enums.ProfileVisibility.Public;
    public DateTimeOffset? LastSeenAt { get; set; }
}
