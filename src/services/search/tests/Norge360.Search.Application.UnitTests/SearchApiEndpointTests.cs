// <copyright file="SearchApiEndpointTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Norge360.Search.API.Endpoints;
using Norge360.Search.API.Security;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Application.Queries;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class SearchApiEndpointTests
{
    [Fact]
    public void MapSearchEndpoints_ShouldAllowAnonymous()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ISearchAccessContextFactory, HttpSearchAccessContextFactory>();
        builder.Services.AddSingleton<ISearchQueryService, FakeSearchQueryService>();
        var app = builder.Build();
        IEndpointRouteBuilder routeBuilder = app;

        app.MapSearchEndpoints();

        var routeEndpoint = routeBuilder.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(endpoint =>
                string.Equals(endpoint.RoutePattern.RawText, "/api/v1/search", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(endpoint.RoutePattern.RawText, "api/v1/search", StringComparison.OrdinalIgnoreCase));

        routeEndpoint.Should().NotBeNull();
        routeEndpoint!.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();

        var suggestRouteEndpoint = routeBuilder.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(endpoint =>
                string.Equals(endpoint.RoutePattern.RawText, "/api/v1/search/suggest", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(endpoint.RoutePattern.RawText, "api/v1/search/suggest", StringComparison.OrdinalIgnoreCase));
        suggestRouteEndpoint.Should().NotBeNull();
        suggestRouteEndpoint!.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
    }

    [Fact]
    public void AccessContextFactory_WhenAnonymousPrincipal_ShouldReturnAnonymousContext()
    {
        var factory = new HttpSearchAccessContextFactory();

        var context = factory.Create(new ClaimsPrincipal(new ClaimsIdentity()));

        context.IsAuthenticated.Should().BeFalse();
        context.TenantId.Should().BeNull();
        context.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void AccessContextFactory_WhenAuthenticatedPrincipal_ShouldReadTenantAndPermissions()
    {
        var tenantId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString("D")),
                new Claim("tenant_id", tenantId.ToString("D")),
                new Claim("permission", "crm.customers.read"),
                new Claim("permissions", "crm.deals.read, *")
            ],
            authenticationType: "test"));
        var factory = new HttpSearchAccessContextFactory();

        var context = factory.Create(principal);

        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().Be(tenantId);
        context.Permissions.Should().Contain("crm.customers.read");
        context.Permissions.Should().Contain("crm.deals.read");
        context.Permissions.Should().Contain("*");
    }

    [Fact]
    public void AccessContextFactory_WhenTenantClaimMissing_ShouldReturnAuthenticatedWithNullTenant()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString("D")),
                new Claim("permission", "account.profile.read")
            ],
            authenticationType: "test"));
        var factory = new HttpSearchAccessContextFactory();

        var context = factory.Create(principal);

        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().BeNull();
        context.Permissions.Should().ContainSingle().Which.Should().Be("account.profile.read");
    }

    [Fact]
    public async Task HandleSearchAsync_WhenAnonymous_ShouldPassAnonymousAccessContextToQueryService()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["q"] = "pricing"
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        _ = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);

        queryService.CallCount.Should().Be(1);
        queryService.LastAccessContext.Should().NotBeNull();
        queryService.LastAccessContext!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task HandleSearchAsync_WhenAuthenticated_ShouldPassAuthenticatedAccessContextToQueryService()
    {
        var tenantId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString("D")),
                new Claim("tenant_id", tenantId.ToString("D")),
                new Claim("permission", "crm.customers.read")
            ],
            authenticationType: "test"));
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["q"] = "customers"
        }, principal);
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        _ = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);

        queryService.LastAccessContext.Should().NotBeNull();
        queryService.LastAccessContext!.IsAuthenticated.Should().BeTrue();
        queryService.LastAccessContext.TenantId.Should().Be(tenantId);
        queryService.LastAccessContext.Permissions.Should().Contain("crm.customers.read");
    }

    [Fact]
    public async Task HandleSearchAsync_ShouldMapQueryParametersToSearchRequest()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["query"] = "fallback",
            ["q"] = "pricing",
            ["source"] = new StringValues(["Public,Tools", "Crm"]),
            ["type"] = "page",
            ["locale"] = "tr",
            ["tags"] = new StringValues(["marketing,pricing", "public"]),
            ["page"] = "2",
            ["pageSize"] = "30",
            ["sort"] = "updatedAtUtc:desc"
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        _ = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);

        queryService.LastRequest.Should().NotBeNull();
        queryService.LastRequest!.Query.Should().Be("pricing");
        queryService.LastRequest.Page.Should().Be(2);
        queryService.LastRequest.PageSize.Should().Be(30);
        queryService.LastRequest.Type.Should().Be("page");
        queryService.LastRequest.Locale.Should().Be("tr-TR");
        queryService.LastRequest.Sort.Should().Be("updatedAtUtc:desc");
        queryService.LastRequest.Sources.Should().BeEquivalentTo(new[]
        {
            SearchDocumentSource.Public,
            SearchDocumentSource.Tools,
            SearchDocumentSource.Crm
        });
        queryService.LastRequest.Tags.Should().BeEquivalentTo("marketing", "pricing", "public");
    }

    [Fact]
    public async Task HandleSearchAsync_ShouldParseSourcesParameter_WithRepeatedAndDelimitedValues()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["sources"] = new StringValues(["Public;Tools", "Crm,Public"])
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        _ = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);

        queryService.LastRequest.Should().NotBeNull();
        queryService.LastRequest!.Sources.Should().BeEquivalentTo(new[]
        {
            SearchDocumentSource.Public,
            SearchDocumentSource.Tools,
            SearchDocumentSource.Crm
        });
    }

    [Fact]
    public async Task HandleSearchAsync_ShouldParseTagsParameter_WithRepeatedAndDelimitedValues()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["tags"] = new StringValues(["alpha,beta", "gamma;alpha"])
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        _ = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);

        queryService.LastRequest.Should().NotBeNull();
        queryService.LastRequest!.Tags.Should().BeEquivalentTo("alpha", "beta", "gamma");
    }

    [Theory]
    [InlineData("page", "invalid-page")]
    [InlineData("pageSize", "invalid-page-size")]
    public async Task HandleSearchAsync_WhenNumericQueryValueIsInvalid_ShouldReturnBadRequestAndSkipServiceCall(string key, string value)
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            [key] = value
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        var result = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        queryService.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleSearchAsync_WhenSourceIsInvalid_ShouldReturnBadRequestAndSkipServiceCall()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["source"] = "invalid-source"
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        var result = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        queryService.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleSearchAsync_ResponsePayload_ShouldNotContainContentField()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["q"] = "pricing"
        });
        var queryService = new FakeSearchQueryService
        {
            Response = new SearchResponse(
                Query: "pricing",
                Page: 1,
                PageSize: 20,
                TotalCount: 1,
                Items:
                [
                    new SearchResultItem(
                        Id: "public:page:pricing",
                        Source: SearchDocumentSource.Public,
                        Type: "page",
                        Title: "Pricing",
                        Summary: "Plans and pricing",
                        Url: "/pricing",
                        Visibility: SearchDocumentVisibility.Public,
                        Locale: "en-US",
                        Tags: ["pricing"],
                        RankingScore: 1.0)
                ],
                PermissionPostFilteringApplied: false)
        };
        var factory = new HttpSearchAccessContextFactory();

        var result = await SearchEndpoints.HandleSearchAsync(httpContext, factory, queryService, CancellationToken.None);
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var payload = valueResult.Value.Should().BeOfType<SearchResponse>().Subject;
        var serialized = JsonSerializer.Serialize(payload);

        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        using var document = JsonDocument.Parse(serialized);
        var itemsProperty = document.RootElement.TryGetProperty("items", out var camelItems)
            ? camelItems
            : document.RootElement.GetProperty("Items");
        var firstItem = itemsProperty[0];
        firstItem.TryGetProperty("content", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("b", 5)]
    [InlineData("be", 8)]
    [InlineData("ber", 12)]
    public async Task HandleSuggestAsync_ShouldApplyMinimumCharacterPolicy(string query, int expectedPageSize)
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>
        {
            ["q"] = query
        });
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        var result = await SearchEndpoints.HandleSuggestAsync(httpContext, factory, queryService, CancellationToken.None);
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        queryService.CallCount.Should().Be(1);
        queryService.LastRequest.Should().NotBeNull();
        queryService.LastRequest!.Type.Should().Be("user");
        queryService.LastRequest.Sources.Should().BeEquivalentTo([SearchDocumentSource.Forum]);
        queryService.LastRequest.Page.Should().Be(1);
        queryService.LastRequest.PageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public async Task HandleSuggestAsync_WhenQueryMissing_ShouldReturnBadRequestAndSkipServiceCall()
    {
        var httpContext = CreateHttpContext(new Dictionary<string, StringValues>());
        var queryService = new FakeSearchQueryService();
        var factory = new HttpSearchAccessContextFactory();

        var result = await SearchEndpoints.HandleSuggestAsync(httpContext, factory, queryService, CancellationToken.None);
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        queryService.CallCount.Should().Be(0);
    }

    private static DefaultHttpContext CreateHttpContext(
        IReadOnlyDictionary<string, StringValues> query,
        ClaimsPrincipal? principal = null)
    {
        var context = new DefaultHttpContext
        {
            User = principal ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        context.Request.QueryString = QueryString.Create(query.SelectMany(pair => pair.Value, (pair, value) => new KeyValuePair<string, string?>(pair.Key, value)));
        context.Request.Query = new QueryCollection(query.ToDictionary(static pair => pair.Key, static pair => pair.Value));

        return context;
    }

    private sealed class FakeSearchQueryService : ISearchQueryService
    {
        public SearchRequest? LastRequest { get; private set; }
        public SearchAccessContext? LastAccessContext { get; private set; }
        public int CallCount { get; private set; }

        public SearchResponse Response { get; init; } = new(
            Query: string.Empty,
            Page: 1,
            PageSize: 20,
            TotalCount: 0,
            Items: [],
            PermissionPostFilteringApplied: false);

        public Task<SearchResponse> SearchAsync(
            SearchRequest request,
            SearchAccessContext accessContext,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastAccessContext = accessContext;
            CallCount++;
            return Task.FromResult(Response);
        }
    }
}
