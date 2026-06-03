// <copyright file="MeilisearchFilterBuilderTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Search.Application.Filtering;
using Norge360.Search.Application.Queries;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Infrastructure.Meilisearch;

namespace Norge360.Search.Application.UnitTests;

public sealed class MeilisearchFilterBuilderTests
{
    [Fact]
    public void Filter_ShouldAlwaysIncludeIsDeletedFalse()
    {
        var filter = BuildFilter(new SearchRequest(), SearchAccessContext.Anonymous);

        filter.Should().Contain("isDeleted = false");
    }

    [Fact]
    public void AnonymousFilter_ShouldBePublicOnly()
    {
        var filter = BuildFilter(new SearchRequest(), SearchAccessContext.Anonymous);

        filter.Should().Contain("visibility = \"Public\"");
        filter.Should().NotContain("visibility = \"Authenticated\"");
        filter.Should().NotContain("visibility = \"Permission\"");
    }

    [Fact]
    public void AuthenticatedTenantFilter_ShouldContainTenantConstraint()
    {
        var tenantId = Guid.NewGuid();
        var access = new SearchAccessContext(true, null, tenantId, []);

        var filter = BuildFilter(new SearchRequest(), access);

        filter.Should().Contain($"tenantId = \"{tenantId}\"");
        filter.Should().Contain("visibility = \"Tenant\"");
    }

    [Fact]
    public void AuthenticatedPermissionFilter_WithoutTenant_ShouldOnlyAllowGlobalPermissionDocuments()
    {
        var access = new SearchAccessContext(true, null, null, ["crm.customer-management.customers.read"]);

        var filter = BuildFilter(new SearchRequest(), access);

        filter.Should().Contain("(visibility = \"Permission\" AND tenantId IS NULL)");
        filter.Should().NotContain("tenantId = \"");
    }

    [Fact]
    public void AuthenticatedPermissionFilter_WithTenant_ShouldAllowGlobalOrMatchingTenantPermissionDocuments()
    {
        var tenantId = Guid.NewGuid();
        var access = new SearchAccessContext(true, null, tenantId, ["crm.customer-management.customers.read"]);

        var filter = BuildFilter(new SearchRequest(), access);

        filter.Should().Contain(
            $"(visibility = \"Permission\" AND (tenantId IS NULL OR tenantId = \"{tenantId}\"))");
    }

    [Fact]
    public void AnonymousBlockedSourcesRequest_ShouldReturnNoResultsFilter()
    {
        var request = new SearchRequest(Sources: [SearchDocumentSource.Crm]);
        var filter = BuildFilter(request, SearchAccessContext.Anonymous);

        filter.Should().Contain("id = \"__no_results__\"");
        filter.Should().NotContain("source = \"Crm\"");
    }

    [Fact]
    public void Filter_ShouldEscapeStringValues()
    {
        var request = new SearchRequest(
            Type: "nav\"item",
            Tags: ["ops\"team", "core\\prod"]);

        var filter = BuildFilter(request, SearchAccessContext.Anonymous);

        filter.Should().Contain("type = \"nav\\\"item\"");
        filter.Should().Contain("tags = \"ops\\\"team\"");
        filter.Should().Contain("tags = \"core\\\\prod\"");
    }

    [Fact]
    public void LocaleFilter_ShouldIncludeRequestedLocaleAndNeutralDocuments()
    {
        var request = new SearchRequest(Locale: "tr");

        var filter = BuildFilter(request, SearchAccessContext.Anonymous);

        filter.Should().Contain("(locale = \"tr-TR\" OR locale = \"neutral\")");
    }

    [Fact]
    public void LocaleFilter_ShouldNotRestrictLocale_WhenLocaleIsMissing()
    {
        var filter = BuildFilter(new SearchRequest(), SearchAccessContext.Anonymous);

        filter.Should().NotContain("locale =");
    }

    private static string BuildFilter(SearchRequest request, SearchAccessContext access)
    {
        var plan = SearchFilterPlanBuilder.Build(request, access);
        var builder = new MeilisearchFilterBuilder();
        return builder.Build(plan);
    }
}

