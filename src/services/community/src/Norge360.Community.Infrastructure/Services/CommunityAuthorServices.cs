using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;

namespace Norge360.Community.Infrastructure.Services;

internal sealed class AccountsCommunityAuthorProfileProvider(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IInternalServiceRequestSigner requestSigner,
    ILogger<AccountsCommunityAuthorProfileProvider> logger) : ICommunityAuthorProfileProvider
{
    public async Task<IReadOnlyDictionary<Guid, CommunityAuthorSummary>> GetAuthorSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var distinctIds = userIds.Where(static x => x != Guid.Empty).Distinct().Take(100).ToArray();
        if (distinctIds.Length == 0)
        {
            return new Dictionary<Guid, CommunityAuthorSummary>();
        }

        try
        {
            var client = httpClientFactory.CreateClient("accounts-community");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/internal/users/batch-summary")
            {
                Content = JsonContent.Create(new { userIds = distinctIds })
            };

            CopyContextHeaders(request);
            await requestSigner.SignAsync(request, cancellationToken);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Accounts batch-summary request failed with status {StatusCode}", response.StatusCode);
                return BuildFallback(distinctIds, currentUserId);
            }

            var payload = await response.Content.ReadFromJsonAsync<InternalBatchSummaryResponse>(cancellationToken: cancellationToken);
            if (payload?.Items is null)
            {
                return BuildFallback(distinctIds, currentUserId);
            }

            var map = payload.Items.ToDictionary(
                static x => x.UserId,
                x => new CommunityAuthorSummary(
                    x.UserId,
                    x.Username,
                    string.IsNullOrWhiteSpace(x.DisplayName) ? x.Username : x.DisplayName,
                    x.AvatarUrl,
                    x.IsVerified,
                    x.CanViewPosts));

            foreach (var userId in distinctIds)
            {
                map.TryAdd(userId, new CommunityAuthorSummary(userId, null, null, null, false, userId == currentUserId));
            }

            return map;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to resolve community author summaries via batch endpoint.");
            return BuildFallback(distinctIds, currentUserId);
        }
    }

    private void CopyContextHeaders(HttpRequestMessage request)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        if (context.Request.Headers.TryGetValue("authorization", out var authorization))
        {
            request.Headers.TryAddWithoutValidation("authorization", authorization.ToString());
        }

        if (context.Request.Headers.TryGetValue("cookie", out var cookie))
        {
            request.Headers.TryAddWithoutValidation("cookie", cookie.ToString());
        }

        if (context.Request.Headers.TryGetValue("x-correlation-id", out var correlationId))
        {
            request.Headers.TryAddWithoutValidation("x-correlation-id", correlationId.ToString());
        }
    }

    private static IReadOnlyDictionary<Guid, CommunityAuthorSummary> BuildFallback(IEnumerable<Guid> userIds, Guid? currentUserId)
    {
        return userIds.ToDictionary(
            static userId => userId,
            userId => new CommunityAuthorSummary(userId, null, null, null, false, userId == currentUserId));
    }

    private sealed record InternalBatchSummaryItem(
        Guid UserId,
        string? Username,
        string? DisplayName,
        string? AvatarUrl,
        bool IsVerified,
        bool CanViewPosts,
        string? ProfileVisibility);

    private sealed record InternalBatchSummaryResponse(IReadOnlyList<InternalBatchSummaryItem> Items);
}

internal sealed class CommunityVisibilityService(ICommunityAuthorProfileProvider authorProfileProvider) : ICommunityVisibilityService
{
    public async Task<IReadOnlySet<Guid>> FilterVisibleAuthorsAsync(IReadOnlyCollection<Guid> authorUserIds, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var summaries = await authorProfileProvider.GetAuthorSummariesAsync(authorUserIds, currentUserId, cancellationToken);
        return summaries.Values.Where(static x => x.CanViewPosts).Select(static x => x.UserId).ToHashSet();
    }

    public async Task<bool> CanViewAuthorPostsAsync(Guid authorUserId, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var summaries = await authorProfileProvider.GetAuthorSummariesAsync([authorUserId], currentUserId, cancellationToken);
        return summaries.TryGetValue(authorUserId, out var summary) && summary.CanViewPosts;
    }
}
