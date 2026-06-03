using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Infrastructure.Services.Models;

namespace Norge360.Auth.Infrastructure.Services;

public sealed class AccountsAuthUserProfileResolver(
    IHttpClientFactory httpClientFactory,
    ILogger<AccountsAuthUserProfileResolver> logger) : IAuthUserProfileResolver
{
    private const string HttpClientName = "accounts-username-resolver";

    public async Task<AuthUserProfileIdentity?> ResolveAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var requestUri = $"api/accounts/internal/users/{authUserId:D}/identity";

        try
        {
            var response = await client.GetAsync(requestUri, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Accounts identity resolve failed. AuthUserId={AuthUserId} StatusCode={StatusCode}",
                    authUserId,
                    (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<InternalUserIdentityPayload>(cancellationToken: cancellationToken);
            if (payload is null || string.IsNullOrWhiteSpace(payload.UserName))
            {
                return null;
            }

            return new AuthUserProfileIdentity(payload.UserName.Trim());
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                exception,
                "Accounts identity resolve threw. AuthUserId={AuthUserId}",
                authUserId);
            return null;
        }
    }
}
