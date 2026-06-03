// <copyright file="SearchFilterPlanBuilderTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Search.Application.Filtering;
using Norge360.Search.Application.Queries;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class SearchFilterPlanBuilderTests
{
    [Fact]
    public void AnonymousPlan_ShouldOnlyAllowPublicVisibility()
    {
        var plan = BuildPlan(new SearchRequest(), SearchAccessContext.Anonymous);

        plan.AllowPublicVisibility.Should().BeTrue();
        plan.AllowAuthenticatedVisibility.Should().BeFalse();
        plan.AllowTenantVisibility.Should().BeFalse();
        plan.AllowPermissionVisibility.Should().BeFalse();
    }

    [Fact]
    public void AnonymousPlan_ShouldExcludeBlockedSourcesEvenWhenRequested()
    {
        var request = new SearchRequest(
            Sources:
            [
                SearchDocumentSource.Crm,
                SearchDocumentSource.Account,
                SearchDocumentSource.Auth,
                SearchDocumentSource.Admin
            ]);

        var plan = BuildPlan(request, SearchAccessContext.Anonymous);

        plan.EffectiveSources.Should().BeEmpty();
    }

    [Fact]
    public void AuthenticatedPlan_ShouldAllowPublicAndAuthenticatedVisibility()
    {
        var access = new SearchAccessContext(true, null, null, []);
        var plan = BuildPlan(new SearchRequest(), access);

        plan.AllowPublicVisibility.Should().BeTrue();
        plan.AllowAuthenticatedVisibility.Should().BeTrue();
    }

    [Fact]
    public void TenantVisibility_ShouldBeEnabledOnlyWhenTenantExists()
    {
        var noTenantPlan = BuildPlan(new SearchRequest(), new SearchAccessContext(true, null, null, []));
        var withTenantPlan = BuildPlan(new SearchRequest(), new SearchAccessContext(true, null, Guid.NewGuid(), []));

        noTenantPlan.AllowTenantVisibility.Should().BeFalse();
        withTenantPlan.AllowTenantVisibility.Should().BeTrue();
    }

    [Fact]
    public void AuthenticatedWithoutTenant_ShouldAllowOnlyGlobalPermissionCandidates()
    {
        var plan = BuildPlan(
            new SearchRequest(),
            new SearchAccessContext(true, null, null, ["crm.customer-management.customers.read"]));

        plan.AllowPermissionVisibility.Should().BeTrue();
        plan.AllowTenantVisibility.Should().BeFalse();
        plan.TenantId.Should().BeNull();
    }

    [Fact]
    public void AuthenticatedWithTenant_ShouldAllowTenantVisibilityAndPermissionCandidates()
    {
        var tenantId = Guid.NewGuid();
        var plan = BuildPlan(
            new SearchRequest(),
            new SearchAccessContext(true, null, tenantId, ["crm.customer-management.customers.read"]));

        plan.AllowPermissionVisibility.Should().BeTrue();
        plan.AllowTenantVisibility.Should().BeTrue();
        plan.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void RequestedSources_ShouldBeIntersectedWithAllowedSources()
    {
        var request = new SearchRequest(
            Sources:
            [
                SearchDocumentSource.Public,
                SearchDocumentSource.Crm,
                SearchDocumentSource.Tools
            ]);

        var plan = BuildPlan(request, SearchAccessContext.Anonymous);

        plan.EffectiveSources.Should().BeEquivalentTo([SearchDocumentSource.Public, SearchDocumentSource.Tools]);
        plan.EffectiveSources.Should().NotContain(SearchDocumentSource.Crm);
    }

    [Fact]
    public void TypeLocaleAndTags_ShouldBeNormalizedAndApplied()
    {
        var request = new SearchRequest(
            Type: "  page  ",
            Locale: "  tr  ",
            Tags: ["  docs  ", "Docs", " howto "]);

        var plan = BuildPlan(request, SearchAccessContext.Anonymous);

        plan.Type.Should().Be("page");
        plan.Locale.Should().Be("tr-TR");
        plan.Tags.Should().BeEquivalentTo(["docs", "howto"]);
    }

    [Theory]
    [InlineData("tr", "tr-TR")]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("en", "en-US")]
    [InlineData("en-US", "en-US")]
    [InlineData("zh-CN", "en-US")]
    public void Locale_ShouldBeCanonicalizedToSupportedLocale(string input, string expected)
    {
        var request = new SearchRequest(Locale: input);

        var plan = BuildPlan(request, SearchAccessContext.Anonymous);

        plan.Locale.Should().Be(expected);
    }

    [Fact]
    public void Locale_ShouldNotRestrict_WhenRequestLocaleIsMissing()
    {
        var plan = BuildPlan(new SearchRequest(Locale: " "), SearchAccessContext.Anonymous);

        plan.Locale.Should().BeNull();
    }

    [Fact]
    public void IncludeDeleted_ShouldAlwaysBeForcedToFalse()
    {
        var request = new SearchRequest(IncludeDeleted: true);
        var plan = BuildPlan(request, new SearchAccessContext(true, null, Guid.NewGuid(), ["crm.read"]));

        plan.IncludeDeleted.Should().BeFalse();
    }

    [Fact]
    public void AuthenticatedPlan_ShouldRequirePermissionPostFiltering()
    {
        var plan = BuildPlan(new SearchRequest(), new SearchAccessContext(true, null, Guid.NewGuid(), ["crm.read"]));

        plan.RequiresPermissionPostFiltering.Should().BeTrue();
        plan.RequiresVisibilityPostFiltering.Should().BeTrue();
    }

    [Fact]
    public void Defaults_ShouldApplyForInvalidPageAndPageSize()
    {
        var request = new SearchRequest(Page: 0, PageSize: -1);
        var plan = BuildPlan(request, SearchAccessContext.Anonymous);

        plan.Page.Should().Be(SearchRequestDefaults.DefaultPage);
        plan.PageSize.Should().Be(SearchRequestDefaults.DefaultPageSize);
    }

    [Fact]
    public void PageSize_ShouldBeCappedToMax()
    {
        var request = new SearchRequest(Page: 2, PageSize: SearchRequestDefaults.MaxPageSize + 500);
        var plan = BuildPlan(request, SearchAccessContext.Anonymous);

        plan.Page.Should().Be(2);
        plan.PageSize.Should().Be(SearchRequestDefaults.MaxPageSize);
    }

    private static SearchFilterPlan BuildPlan(SearchRequest request, SearchAccessContext access) =>
        SearchFilterPlanBuilder.Build(request, access);
}

