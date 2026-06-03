// <copyright file="AccountsDiscoveryBackfillService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Norge360.Discovery.API.Options;
using Norge360.Discovery.API.Security;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;

namespace Norge360.Discovery.API.Services;

public sealed class AccountsDiscoveryBackfillService(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscoveryAccountsOptions> accountsOptions,
    IOptions<DiscoveryInternalEventOptions> internalEventOptions,
    IDiscoverySnapshotService snapshotService,
    ILogger<AccountsDiscoveryBackfillService> logger) : IAccountsDiscoveryBackfillService
{
    public async Task<DiscoveryBackfillResponse> BackfillAsync(int take, int maxBatches, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 500);
        var safeMaxBatches = Math.Clamp(maxBatches, 1, 10000);
        var client = httpClientFactory.CreateClient("accounts-discovery-export");
        var processed = 0;
        var created = 0;
        var updated = 0;
        var invalid = 0;
        var batches = 0;
        DateTimeOffset? cursorUpdatedAt = null;
        Guid? cursorProfileId = null;

        while (batches < safeMaxBatches)
        {
            var uri = BuildExportUri(safeTake, cursorUpdatedAt, cursorProfileId);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            AddInternalTokenHeader(request);

            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<AccountsDiscoveryProfileExportResponse>(cancellationToken: cancellationToken);
            if (page is null || page.Items.Count == 0)
            {
                break;
            }

            var upsert = await snapshotService.UpsertBatchAsync(
                new DiscoverySnapshotBatchRequest(page.Items.Select(MapSnapshot).ToArray()),
                cancellationToken);

            processed += upsert.Accepted;
            created += upsert.Created;
            updated += upsert.Updated;
            invalid += upsert.Invalid;
            batches++;

            logger.LogInformation(
                "Discovery account profile backfill batch completed. Batch={Batch} Accepted={Accepted} Created={Created} Updated={Updated} Invalid={Invalid}",
                batches,
                upsert.Accepted,
                upsert.Created,
                upsert.Updated,
                upsert.Invalid);

            if (!page.HasMore || !page.NextUpdatedAt.HasValue || !page.NextProfileId.HasValue)
            {
                break;
            }

            cursorUpdatedAt = page.NextUpdatedAt;
            cursorProfileId = page.NextProfileId;
        }

        return new DiscoveryBackfillResponse(processed, created, updated, invalid, batches);
    }

    private void AddInternalTokenHeader(HttpRequestMessage request)
    {
        var options = accountsOptions.Value;
        var token = options.InternalToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            token = internalEventOptions.Value.Token;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Accounts discovery export internal token is not configured.");
        }

        var headerName = string.IsNullOrWhiteSpace(options.InternalTokenHeaderName)
            ? "X-Discovery-Internal-Token"
            : options.InternalTokenHeaderName;
        request.Headers.TryAddWithoutValidation(headerName, token);
    }

    private static string BuildExportUri(int take, DateTimeOffset? cursorUpdatedAt, Guid? cursorProfileId)
    {
        var query = $"?take={take}";
        if (cursorUpdatedAt.HasValue && cursorProfileId.HasValue)
        {
            query += $"&cursorUpdatedAt={Uri.EscapeDataString(cursorUpdatedAt.Value.ToString("O"))}&cursorProfileId={cursorProfileId.Value}";
        }

        return "/api/accounts/internal/discovery/profiles" + query;
    }

    private static DiscoverySnapshotUpsertRequest MapSnapshot(AccountsDiscoveryProfileExportItem item) => new(
        item.ProfileId,
        item.AuthUserId,
        item.Username,
        item.DisplayName,
        item.AvatarUrl,
        item.Bio,
        item.IsVerified,
        item.Visibility,
        item.IsActive,
        item.IsDeleted,
        item.FollowersCount,
        item.PostsCount,
        item.UpdatedAt);

    private sealed record AccountsDiscoveryProfileExportResponse(
        IReadOnlyList<AccountsDiscoveryProfileExportItem> Items,
        DateTimeOffset? NextUpdatedAt,
        Guid? NextProfileId,
        bool HasMore);

    private sealed record AccountsDiscoveryProfileExportItem(
        Guid ProfileId,
        Guid? AuthUserId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        string? Bio,
        bool IsVerified,
        string Visibility,
        bool IsActive,
        bool IsDeleted,
        int FollowersCount,
        int PostsCount,
        DateTimeOffset UpdatedAt);
}
