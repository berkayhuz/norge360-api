// <copyright file="SearchDocumentVisibilityEvaluator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Security;

public static class SearchDocumentVisibilityEvaluator
{
    public static bool CanAccess(SearchDocument document, SearchAccessContext access)
    {
        if (!SearchDocumentSecurityValidator.IsValid(document))
        {
            return false;
        }

        if (!access.IsAuthenticated)
        {
            return SearchDocumentSecurityValidator.IsAllowedInAnonymousResults(document);
        }

        return document.Visibility switch
        {
            SearchDocumentVisibility.Public => true,
            SearchDocumentVisibility.Authenticated => true,
            SearchDocumentVisibility.Tenant => CanAccessTenantDocument(document, access),
            SearchDocumentVisibility.Permission => CanAccessPermissionDocument(document, access),
            _ => false
        };
    }

    private static bool CanAccessTenantDocument(SearchDocument document, SearchAccessContext access)
    {
        if (document.TenantId is null)
        {
            return SearchDocumentSecurityValidator.IsTenantVisibilityWithoutTenantIdAllowed(document);
        }

        return access.TenantId.HasValue && access.TenantId.Value == document.TenantId.Value;
    }

    private static bool CanAccessPermissionDocument(SearchDocument document, SearchAccessContext access)
    {
        if (document.TenantId.HasValue)
        {
            if (!access.TenantId.HasValue || access.TenantId.Value != document.TenantId.Value)
            {
                return false;
            }
        }

        var requiredPermissions = document.RequiredPermissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requiredPermissions.Length == 0)
        {
            return false;
        }

        var grantedPermissions = access.Permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (grantedPermissions.Contains("*"))
        {
            return true;
        }

        return document.PermissionMatchMode == SearchPermissionMatchMode.All
            ? requiredPermissions.All(grantedPermissions.Contains)
            : requiredPermissions.Any(grantedPermissions.Contains);
    }
}
