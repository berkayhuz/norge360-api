// <copyright file="GatewayRouteConfigurationTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using FluentAssertions;

namespace Norge360.ApiGateway.UnitTests.Configuration;

public sealed class GatewayRouteConfigurationTests
{
    [Theory]
    [InlineData("platform/gateway/src/Norge360.ApiGateway/appsettings.json", true)]
    [InlineData("platform/gateway/src/Norge360.ApiGateway/appsettings.Development.json", false)]
    public void Config_Should_Expose_Search_Route_And_Optional_Community_Route(string configurationPath, bool expectCommunityRoute)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FindRepoFile(configurationPath)));
        var reverseProxy = document.RootElement.GetProperty("ReverseProxy");
        var routes = reverseProxy.GetProperty("Routes");
        var clusters = reverseProxy.GetProperty("Clusters");

        routes.TryGetProperty("search-api-route", out var searchRoute).Should().BeTrue();
        searchRoute.GetProperty("ClusterId").GetString().Should().Be("search-api-cluster");
        searchRoute.GetProperty("Match").GetProperty("Path").GetString().Should().Be("/api/v1/search/{**catch-all}");

        if (expectCommunityRoute)
        {
            routes.TryGetProperty("community-api-route", out var communityRoute).Should().BeTrue();
            communityRoute.GetProperty("ClusterId").GetString().Should().Be("community-api-cluster");
            communityRoute.GetProperty("Match").GetProperty("Path").GetString().Should().Be("/api/community/{**catch-all}");
        }
        else
        {
            routes.TryGetProperty("community-api-route", out _).Should().BeFalse();
        }

        clusters.TryGetProperty("search-api-cluster", out var searchCluster).Should().BeTrue();
        searchCluster
            .GetProperty("Destinations")
            .GetProperty("destination1")
            .GetProperty("Address")
            .GetString()
            .Should()
            .Be("http://localhost:64515/");
    }

    [Theory]
    [InlineData("platform/gateway/src/Norge360.ApiGateway/appsettings.json")]
    [InlineData("platform/gateway/src/Norge360.ApiGateway/appsettings.Development.json")]
    public void Config_Should_Keep_Existing_Core_Api_Routes(string configurationPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FindRepoFile(configurationPath)));
        var routes = document.RootElement.GetProperty("ReverseProxy").GetProperty("Routes");

        routes.TryGetProperty("auth-api-route", out _).Should().BeTrue();
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.");
    }
}
