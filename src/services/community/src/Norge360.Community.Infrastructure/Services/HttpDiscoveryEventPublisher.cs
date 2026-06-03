using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Infrastructure.Options;

namespace Norge360.Community.Infrastructure.Services;

public sealed class HttpDiscoveryEventPublisher(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscoveryApiOptions> options,
    ILogger<HttpDiscoveryEventPublisher> logger) : IDiscoveryEventPublisher
{
    public async Task PublishAsync(DiscoveryEventEnvelope discoveryEvent, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("discovery-community");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/discovery/internal/events")
        {
            Content = JsonContent.Create(discoveryEvent)
        };
        AddInternalTokenHeader(request);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Discovery event publish request failed. EventType={EventType} SourceEntityType={SourceEntityType}",
                discoveryEvent.EventType,
                discoveryEvent.SourceEntityType);
            throw;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Discovery event publish failed. EventType={EventType} SourceEntityType={SourceEntityType} StatusCode={StatusCode}",
                    discoveryEvent.EventType,
                    discoveryEvent.SourceEntityType,
                    response.StatusCode);
            }

            response.EnsureSuccessStatusCode();
        }
    }

    private void AddInternalTokenHeader(HttpRequestMessage request)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.InternalToken))
        {
            logger.LogWarning("Discovery internal token is not configured for Community publisher.");
            return;
        }

        var headerName = string.IsNullOrWhiteSpace(value.InternalTokenHeaderName)
            ? "X-Discovery-Internal-Token"
            : value.InternalTokenHeaderName;
        request.Headers.TryAddWithoutValidation(headerName, value.InternalToken);
    }
}

public sealed class NoOpDiscoveryEventPublisher : IDiscoveryEventPublisher
{
    public Task PublishAsync(DiscoveryEventEnvelope discoveryEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
