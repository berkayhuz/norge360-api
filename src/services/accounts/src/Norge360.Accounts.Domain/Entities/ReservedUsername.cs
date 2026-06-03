// <copyright file="ReservedUsername.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Domain.Entities;

public sealed class ReservedUsername
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NormalizedValue { get; set; } = null!;
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
