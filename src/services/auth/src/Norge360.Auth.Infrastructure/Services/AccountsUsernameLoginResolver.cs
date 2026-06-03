// <copyright file="AccountsUsernameLoginResolver.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Infrastructure.Services.Models;

namespace Norge360.Auth.Infrastructure.Services;

public sealed class AccountsUsernameLoginResolver(
    IHttpClientFactory httpClientFactory,
    ILogger<AccountsUsernameLoginResolver> logger) : IUsernameLoginResolver
{
    private const string ClientName = "accounts-username-resolver";

    public async Task<Guid?> ResolveAuthUserIdAsync(string normalizedUsername, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedUsername);

        var client = httpClientFactory.CreateClient(ClientName);
        var requestUri = $"api/accounts/internal/users/resolve-by-username/{Uri.EscapeDataString(normalizedUsername)}";

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(requestUri, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Username resolution request failed. Username={Username}",
                normalizedUsername);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Username resolution responded with non-success status code. Username={Username} StatusCode={StatusCode}",
                normalizedUsername,
                (int)response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<InternalAuthUserResolutionPayload>(cancellationToken: cancellationToken);
        return payload?.AuthUserId;
    }
}
