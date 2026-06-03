// <copyright file="MeilisearchFilterBuilder.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Application.Filtering;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Infrastructure.Meilisearch;

public sealed class MeilisearchFilterBuilder
{
    public string Build(SearchFilterPlan plan)
    {
        var conditions = new List<string>
        {
            "isDeleted = false"
        };

        AddSourceFilter(plan, conditions);
        AddVisibilityFilter(plan, conditions);
        AddTypeFilter(plan, conditions);
        AddLocaleFilter(plan, conditions);
        AddTagsFilter(plan, conditions);

        return string.Join(" AND ", conditions);
    }

    private static void AddSourceFilter(SearchFilterPlan plan, ICollection<string> conditions)
    {
        if (plan.EffectiveSources.Count == 0)
        {
            conditions.Add("id = \"__no_results__\"");
            return;
        }

        var sourceExpressions = plan.EffectiveSources
            .Select(source => $"source = {Quote(source.ToString())}")
            .ToArray();

        conditions.Add($"({string.Join(" OR ", sourceExpressions)})");
    }

    private static void AddVisibilityFilter(SearchFilterPlan plan, ICollection<string> conditions)
    {
        if (!plan.IsAuthenticated)
        {
            conditions.Add($"visibility = {Quote(SearchDocumentVisibility.Public.ToString())}");
            return;
        }

        var visibilityClauses = new List<string>();

        if (plan.AllowPublicVisibility)
        {
            visibilityClauses.Add($"visibility = {Quote(SearchDocumentVisibility.Public.ToString())}");
        }

        if (plan.AllowAuthenticatedVisibility)
        {
            visibilityClauses.Add($"visibility = {Quote(SearchDocumentVisibility.Authenticated.ToString())}");
        }

        if (plan.AllowTenantVisibility && plan.TenantId.HasValue)
        {
            visibilityClauses.Add($"(visibility = {Quote(SearchDocumentVisibility.Tenant.ToString())} AND tenantId = {Quote(plan.TenantId.Value.ToString())})");
        }

        if (plan.AllowTenantNavigationWithoutTenantId)
        {
            visibilityClauses.Add($"(visibility = {Quote(SearchDocumentVisibility.Tenant.ToString())} AND source = {Quote(SearchDocumentSource.Platform.ToString())} AND type = {Quote("navigation")} AND tenantId IS NULL)");
        }

        if (plan.AllowPermissionVisibility)
        {
            if (plan.TenantId.HasValue)
            {
                visibilityClauses.Add(
                    $"(visibility = {Quote(SearchDocumentVisibility.Permission.ToString())} AND (tenantId IS NULL OR tenantId = {Quote(plan.TenantId.Value.ToString())}))");
            }
            else
            {
                visibilityClauses.Add(
                    $"(visibility = {Quote(SearchDocumentVisibility.Permission.ToString())} AND tenantId IS NULL)");
            }
        }

        if (visibilityClauses.Count == 0)
        {
            conditions.Add("id = \"__no_results__\"");
            return;
        }

        conditions.Add($"({string.Join(" OR ", visibilityClauses)})");
    }

    private static void AddTypeFilter(SearchFilterPlan plan, ICollection<string> conditions)
    {
        if (plan.Type is null)
        {
            return;
        }

        conditions.Add($"type = {Quote(plan.Type)}");
    }

    private static void AddLocaleFilter(SearchFilterPlan plan, ICollection<string> conditions)
    {
        if (plan.Locale is null)
        {
            return;
        }

        conditions.Add(
            $"(locale = {Quote(plan.Locale)} OR locale = {Quote(SearchDocumentLocales.Neutral)})");
    }

    private static void AddTagsFilter(SearchFilterPlan plan, ICollection<string> conditions)
    {
        if (plan.Tags.Count == 0)
        {
            return;
        }

        var tagClauses = plan.Tags
            .Select(tag => $"tags = {Quote(tag)}")
            .ToArray();

        conditions.Add($"({string.Join(" AND ", tagClauses)})");
    }

    private static string Quote(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return $"\"{escaped}\"";
    }
}
