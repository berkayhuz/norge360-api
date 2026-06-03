// <copyright file="MeilisearchReadinessHealthCheck.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Infrastructure.Health;

public sealed class MeilisearchReadinessHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<SearchOptions> searchOptions,
    IOptions<MeilisearchOptions> meilisearchOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = searchOptions.Value.Provider;
        if (!provider.Equals("Meilisearch", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Healthy($"Search provider '{provider}' does not require Meilisearch readiness check.");
        }

        var endpoint = meilisearchOptions.Value.Endpoint.TrimEnd('/');
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseUri))
        {
            return HealthCheckResult.Unhealthy("Meilisearch endpoint is invalid.");
        }

        var healthUri = new Uri(baseUri, "/health");
        var client = httpClientFactory.CreateClient(MeilisearchOptions.HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, healthUri);
            using var response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Meilisearch is reachable.")
                : HealthCheckResult.Unhealthy($"Meilisearch health endpoint returned status {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Meilisearch is not reachable.", exception);
        }
    }
}
