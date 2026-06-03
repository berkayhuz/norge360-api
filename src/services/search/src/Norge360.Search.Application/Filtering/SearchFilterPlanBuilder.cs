// <copyright file="SearchFilterPlanBuilder.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Application.Queries;
using Norge360.Search.Application.Security;
using Norge360.Search.Application.Localization;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.Filtering;

public static class SearchFilterPlanBuilder
{
    private static readonly SearchDocumentSource[] AnonymousBlockedSources =
    [
        SearchDocumentSource.Crm,
        SearchDocumentSource.Account,
        SearchDocumentSource.Auth,
        SearchDocumentSource.Admin
    ];

    private static readonly SearchDocumentSource[] AllSources =
    [
        SearchDocumentSource.Public,
        SearchDocumentSource.Tools,
        SearchDocumentSource.Account,
        SearchDocumentSource.Auth,
        SearchDocumentSource.Crm,
        SearchDocumentSource.Forum,
        SearchDocumentSource.Admin,
        SearchDocumentSource.Platform
    ];

    public static SearchFilterPlan Build(SearchRequest request, SearchAccessContext accessContext)
    {
        var normalizedQuery = (request.Query ?? string.Empty).Trim();
        var page = request.Page.GetValueOrDefault(SearchRequestDefaults.DefaultPage);
        page = page <= 0 ? SearchRequestDefaults.DefaultPage : page;

        var pageSize = request.PageSize.GetValueOrDefault(SearchRequestDefaults.DefaultPageSize);
        pageSize = pageSize <= 0 ? SearchRequestDefaults.DefaultPageSize : Math.Min(pageSize, SearchRequestDefaults.MaxPageSize);

        var permissions = accessContext.Permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var hasWildcardPermission = permissions.Contains("*", StringComparer.OrdinalIgnoreCase);

        var allowedSources = accessContext.IsAuthenticated
            ? AllSources
            : AllSources.Where(source => !AnonymousBlockedSources.Contains(source)).ToArray();

        var requestedSources = (request.Sources ?? [])
            .Distinct()
            .ToArray();

        var effectiveSources = requestedSources.Length == 0
            ? allowedSources
            : requestedSources.Intersect(allowedSources).ToArray();

        var normalizedType = NormalizeSingleValue(request.Type);
        var normalizedLocale = SearchLocale.CanonicalizeRequestedLocale(request.Locale);
        var normalizedTags = NormalizeMultiValue(request.Tags);
        var normalizedSort = NormalizeSingleValue(request.Sort);

        var allowPublicVisibility = true;
        var allowAuthenticatedVisibility = accessContext.IsAuthenticated;
        var allowTenantVisibility = accessContext.IsAuthenticated && accessContext.TenantId.HasValue;
        var allowTenantNavigationWithoutTenantId = accessContext.IsAuthenticated;
        var allowPermissionVisibility = accessContext.IsAuthenticated && (permissions.Length > 0 || hasWildcardPermission);

        var requiresPermissionPostFiltering = accessContext.IsAuthenticated;
        var requiresVisibilityPostFiltering = accessContext.IsAuthenticated;

        return new SearchFilterPlan(
            Query: normalizedQuery,
            Page: page,
            PageSize: pageSize,
            IncludeDeleted: false,
            IsAuthenticated: accessContext.IsAuthenticated,
            TenantId: accessContext.TenantId,
            UserPermissions: permissions,
            HasWildcardPermission: hasWildcardPermission,
            AllowedSources: allowedSources,
            RequestedSources: requestedSources,
            EffectiveSources: effectiveSources,
            Type: normalizedType,
            Locale: normalizedLocale,
            Tags: normalizedTags,
            Sort: normalizedSort,
            AllowPublicVisibility: allowPublicVisibility,
            AllowAuthenticatedVisibility: allowAuthenticatedVisibility,
            AllowTenantVisibility: allowTenantVisibility,
            AllowTenantNavigationWithoutTenantId: allowTenantNavigationWithoutTenantId,
            AllowPermissionVisibility: allowPermissionVisibility,
            RequiresPermissionPostFiltering: requiresPermissionPostFiltering,
            RequiresVisibilityPostFiltering: requiresVisibilityPostFiltering);
    }

    private static string? NormalizeSingleValue(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static IReadOnlyCollection<string> NormalizeMultiValue(IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
