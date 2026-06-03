// <copyright file="UsernameHistory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Domain.Entities;

public sealed class UsernameHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfileId { get; set; }
    public string OldUsername { get; set; } = null!;
    public string NormalizedOldUsername { get; set; } = null!;
    public string NewUsername { get; set; } = null!;
    public string NormalizedNewUsername { get; set; } = null!;
    public DateTimeOffset ChangedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}
