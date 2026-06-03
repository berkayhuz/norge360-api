// <copyright file="SearchAccessContext.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Application.Security;

public sealed record SearchAccessContext(
    bool IsAuthenticated,
    Guid? UserId,
    Guid? TenantId,
    IReadOnlyCollection<string> Permissions)
{
    public static SearchAccessContext Anonymous { get; } = new(false, null, null, []);
}
