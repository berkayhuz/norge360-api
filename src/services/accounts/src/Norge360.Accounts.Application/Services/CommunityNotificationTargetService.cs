// <copyright file="CommunityNotificationTargetService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Services;

public sealed class CommunityNotificationTargetService(
    IProfileNotificationSubscriptionRepository subscriptionRepository,
    IUserBlockRepository userBlockRepository,
    IUserFollowRepository userFollowRepository,
    IUserProfileRepository userProfileRepository) : ICommunityNotificationTargetService
{
    public async Task<CommunityNotificationTargetsResponse> ResolveAsync(
        CommunityNotificationTargetsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AuthorUserId == Guid.Empty)
        {
            return new CommunityNotificationTargetsResponse([], [], []);
        }

        var authorProfile = await userProfileRepository.GetByAuthUserIdAsync(
            request.AuthorUserId,
            includeDeleted: false,
            cancellationToken);
        if (authorProfile is null || !authorProfile.IsActive)
        {
            return new CommunityNotificationTargetsResponse([], [], []);
        }

        var safeLimit = Math.Clamp(request.MaxRecipients, 1, 1000);
        var excludedProfileIds = await BuildExcludedProfileIdsAsync(authorProfile.Id, cancellationToken);
        IReadOnlyCollection<Guid> followers = request.IncludeFollowers
            ? await userFollowRepository.ListFollowerAuthUserIdsAsync(authorProfile.Id, safeLimit, cancellationToken)
            : Array.Empty<Guid>();
        IReadOnlyCollection<Guid> subscribers = request.IncludeProfileSubscribers
            ? await subscriptionRepository.ListSubscriberAuthUserIdsAsync(authorProfile.Id, safeLimit, cancellationToken)
            : Array.Empty<Guid>();
        IReadOnlyCollection<Guid> cityResidents = request.IncludeCityResidents && !string.IsNullOrWhiteSpace(request.City)
            ? await userProfileRepository.ListAuthUserIdsByCityAsync(
                request.City,
                excludedProfileIds.Append(authorProfile.Id).ToArray(),
                safeLimit,
                cancellationToken)
            : Array.Empty<Guid>();

        return new CommunityNotificationTargetsResponse(
            FilterSelf(followers, request.AuthorUserId, safeLimit),
            FilterSelf(subscribers, request.AuthorUserId, safeLimit),
            FilterSelf(cityResidents, request.AuthorUserId, safeLimit));
    }

    private async Task<IReadOnlyCollection<Guid>> BuildExcludedProfileIdsAsync(
        Guid authorProfileId,
        CancellationToken cancellationToken)
    {
        var blocked = await userBlockRepository.ListBlockedProfileIdsAsync(authorProfileId, cancellationToken);
        var blockers = await userBlockRepository.ListBlockerProfileIdsAsync(authorProfileId, cancellationToken);
        return blocked.Concat(blockers).Distinct().ToArray();
    }

    private static IReadOnlyList<Guid> FilterSelf(
        IReadOnlyCollection<Guid> userIds,
        Guid authorUserId,
        int limit) =>
        userIds
            .Where(userId => userId != Guid.Empty && userId != authorUserId)
            .Distinct()
            .Take(limit)
            .ToArray();
}
