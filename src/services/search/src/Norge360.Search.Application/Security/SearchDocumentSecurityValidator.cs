// <copyright file="SearchDocumentSecurityValidator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Security;

public static class SearchDocumentSecurityValidator
{
    public static bool IsValid(SearchDocument document) => Validate(document).Count == 0;

    public static IReadOnlyCollection<string> Validate(SearchDocument document)
    {
        var failures = new List<string>();

        if (document.Visibility == SearchDocumentVisibility.Public && IsPublicSourceBlocked(document.Source))
        {
            failures.Add($"Source '{document.Source}' cannot use public visibility.");
        }

        var hasAnyRequiredPermission = document.RequiredPermissions.Any(permission => !string.IsNullOrWhiteSpace(permission));

        if (document.Visibility == SearchDocumentVisibility.Permission && !hasAnyRequiredPermission)
        {
            failures.Add("Permission visibility requires at least one required permission.");
        }

        if (RequiresTenantIdForCrmDynamicEntity(document) && document.TenantId is null)
        {
            failures.Add("CRM dynamic entity documents require TenantId.");
        }

        if (document.Visibility == SearchDocumentVisibility.Tenant &&
            document.TenantId is null &&
            !IsTenantVisibilityWithoutTenantIdAllowed(document))
        {
            failures.Add("Tenant visibility requires TenantId for tenant-owned documents.");
        }

        return failures;
    }

    public static bool IsAllowedInAnonymousResults(SearchDocument document) =>
        document.Visibility == SearchDocumentVisibility.Public &&
        !IsPublicSourceBlocked(document.Source);

    public static bool IsPublicSourceBlocked(SearchDocumentSource source) =>
        source is SearchDocumentSource.Crm or SearchDocumentSource.Account or SearchDocumentSource.Auth or SearchDocumentSource.Admin;

    public static bool IsTenantVisibilityWithoutTenantIdAllowed(SearchDocument document) =>
        document.Visibility == SearchDocumentVisibility.Tenant &&
        document.TenantId is null &&
        document.Source == SearchDocumentSource.Platform &&
        string.Equals(document.Type, "navigation", StringComparison.OrdinalIgnoreCase);

    private static bool RequiresTenantIdForCrmDynamicEntity(SearchDocument document)
    {
        if (document.Source != SearchDocumentSource.Crm)
        {
            return false;
        }

        return document.Type.Trim().ToLowerInvariant() is
            "customer" or "customers" or
            "company" or "companies" or
            "contact" or "contacts" or
            "deal" or "deals" or
            "ticket" or "tickets";
    }
}
