// <copyright file="HttpSearchAccessContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Claims;
using Norge360.Authorization.Claims;
using Norge360.Search.Application.Security;

namespace Norge360.Search.API.Security;

public sealed class HttpSearchAccessContextFactory : ISearchAccessContextFactory
{
    public SearchAccessContext Create(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return SearchAccessContext.Anonymous;
        }

        var tenantId = ReadTenantId(principal);
        var userId = ReadUserId(principal);
        var permissions = PermissionClaimReader.ReadPermissions(principal);

        return new SearchAccessContext(
            IsAuthenticated: true,
            UserId: userId,
            TenantId: tenantId,
            Permissions: permissions);
    }

    private static Guid? ReadUserId(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("sub")?.Value ??
                          principal.FindFirst("user_id")?.Value ??
                          principal.FindFirst("uid")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) && userId != Guid.Empty
            ? userId
            : null;
    }

    private static Guid? ReadTenantId(ClaimsPrincipal principal)
    {
        var tenantClaim = principal.FindFirst("tenant_id")?.Value ??
                          principal.FindFirst("tenantId")?.Value ??
                          principal.FindFirst("tenant")?.Value;

        return Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty
            ? tenantId
            : null;
    }
}

