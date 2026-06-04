// <copyright file="GatewaySearchRouteConfigTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using FluentAssertions;

namespace Norge360.Search.Application.UnitTests;

public sealed class GatewaySearchRouteConfigTests
{
    [Fact]
    public void GatewayAppsettings_ShouldContainSearchRouteBeforeCrmCatchAll()
    {
        var repoRoot = FindRepoRoot();
        var appsettingsPath = Path.Combine(repoRoot, "src", "platform", "gateway", "src", "Norge360.ApiGateway", "appsettings.json");
        var appsettingsJson = File.ReadAllText(appsettingsPath);
        using var document = JsonDocument.Parse(appsettingsJson);

        var routes = document.RootElement.GetProperty("ReverseProxy").GetProperty("Routes");
        var searchRoute = routes.GetProperty("search-api-route");

        searchRoute.GetProperty("ClusterId").GetString().Should().Be("search-api-cluster");
        searchRoute.GetProperty("Match").GetProperty("Path").GetString().Should().Be("/api/v1/search/{**catch-all}");
        appsettingsJson.IndexOf("\"search-api-route\"", StringComparison.Ordinal)
            .Should()
            .BeLessThan(appsettingsJson.IndexOf("\"community-api-route\"", StringComparison.Ordinal));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Norge360.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }
}
