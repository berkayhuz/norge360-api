// <copyright file="AccountsBlockedProfileIdsProvider.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Norge360.Search.Infrastructure.Abstractions;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Infrastructure.Accounts;

internal sealed class AccountsBlockedProfileIdsProvider(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<SearchBlockFilterOptions> options) : IBlockedProfileIdsProvider
{
    public async Task<IReadOnlySet<Guid>> GetRelatedBlockedProfileIdsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || currentUserId == Guid.Empty)
        {
            return new HashSet<Guid>();
        }

        var token = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new HashSet<Guid>();
        }

        var baseUrl = options.Value.AccountsApiBaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/accounts/blocks/me/relations");
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);

        var client = httpClientFactory.CreateClient(nameof(AccountsBlockedProfileIdsProvider));
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new HashSet<Guid>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<BlockRelationsResponse>(stream, cancellationToken: cancellationToken);
        if (payload is null)
        {
            return new HashSet<Guid>();
        }

        return payload.BlockedProfileIds
            .Concat(payload.BlockerProfileIds)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }

    private sealed record BlockRelationsResponse(
        IReadOnlyCollection<Guid> BlockedProfileIds,
        IReadOnlyCollection<Guid> BlockerProfileIds);
}
