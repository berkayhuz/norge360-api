// <copyright file="ProfileQueryService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileQueryService(
    IUserProfileRepository userProfileRepository,
    IUserFollowRepository userFollowRepository,
    IUserBlockRepository userBlockRepository,
    IProfileNotificationSubscriptionRepository profileNotificationSubscriptionRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator,
    IProfileVisibilityPolicy profileVisibilityPolicy) : IProfileQueryService
{
    public async Task<ProfileQueryResult<MyProfileResponse>> GetMyProfileAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty)
        {
            return ProfileQueryResult<MyProfileResponse>.Unauthorized("authenticated_user_required");
        }

        var profile = await userProfileRepository.GetByAuthUserIdAsync(
            authUserId,
            includeDeleted: false,
            cancellationToken);

        return profile is null
            ? ProfileQueryResult<MyProfileResponse>.ProvisioningPending("profile_provisioning_pending")
            : ProfileQueryResult<MyProfileResponse>.Success(
                MapMyProfile(
                    profile,
                    await LoadFollowCountsAsync(profile.Id, cancellationToken)));
    }

    public async Task<ProfileQueryResult<ProfileResponse>> GetPublicProfileByUsernameAsync(
        string username,
        Guid? viewerAuthUserId = null,
        CancellationToken cancellationToken = default)
    {
        var value = username.Trim();
        var validation = usernameValidator.Validate(value);
        if (!validation.IsValid)
        {
            return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
        }

        var normalizedUsername = usernameNormalizer.Normalize(value);
        var profile = await userProfileRepository.GetByNormalizedUsernameAsync(
            normalizedUsername,
            includeDeleted: false,
            cancellationToken);

        if (profile is null || !profile.IsActive)
        {
            return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
        }

        UserProfile? viewerProfile = null;
        if (viewerAuthUserId.HasValue)
        {
            viewerProfile = await userProfileRepository.GetByAuthUserIdAsync(
                viewerAuthUserId.Value,
                includeDeleted: false,
                cancellationToken);
            if (viewerProfile is not null)
            {
                var blocked = await userBlockRepository.ExistsBetweenAsync(
                    viewerProfile.Id,
                    profile.Id,
                    cancellationToken);
                if (blocked)
                {
                    return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
                }
            }
        }

        if (profile.ProfileVisibility == Domain.Enums.ProfileVisibility.Private && !viewerAuthUserId.HasValue)
        {
            return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
        }

        if (profile.ProfileVisibility == Domain.Enums.ProfileVisibility.FollowersOnly)
        {
            if (!viewerAuthUserId.HasValue)
            {
                return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
            }

            if (viewerAuthUserId.Value != profile.AuthUserId)
            {
                if (viewerProfile is null)
                {
                    return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
                }

                var isFollower = await userFollowRepository.ExistsActiveAsync(
                    viewerProfile.Id,
                    profile.Id,
                    cancellationToken);
                if (!isFollower)
                {
                    return ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found");
                }
            }
        }

        var relationship = await ResolveRelationshipAsync(viewerProfile, profile, cancellationToken);
        var followCounts = await LoadFollowCountsAsync(profile.Id, cancellationToken);

        return profileVisibilityPolicy.Evaluate(profile, viewerAuthUserId) switch
        {
            ProfileVisibilityDecision.Full => ProfileQueryResult<ProfileResponse>.Success(MapFullProfile(profile, relationship, followCounts)),
            ProfileVisibilityDecision.Limited => ProfileQueryResult<ProfileResponse>.Success(MapLimitedProfile(profile, relationship, followCounts)),
            _ => ProfileQueryResult<ProfileResponse>.NotFound("profile_not_found")
        };
    }

    public async Task<Guid?> ResolveAuthUserIdByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var value = username.Trim();
        var validation = usernameValidator.Validate(value);
        if (!validation.IsValid)
        {
            return null;
        }

        var normalizedUsername = usernameNormalizer.Normalize(value);
        var profile = await userProfileRepository.GetByNormalizedUsernameAsync(
            normalizedUsername,
            includeDeleted: false,
            cancellationToken);

        return profile is { IsActive: true } ? profile.AuthUserId : null;
    }

    public async Task<string?> ResolveUsernameByAuthUserIdAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty)
        {
            return null;
        }

        var profile = await userProfileRepository.GetByAuthUserIdAsync(
            authUserId,
            includeDeleted: false,
            cancellationToken);

        return profile is { IsActive: true } ? profile.Username : null;
    }

    public async Task<Guid?> ResolveAuthUserIdByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            return null;
        }

        var profile = await userProfileRepository.GetByProfileIdAsync(profileId, includeDeleted: false, cancellationToken);
        return profile is { IsActive: true } ? profile.AuthUserId : null;
    }

    public async Task<InternalUserBatchSummaryResponse> GetInternalUserBatchSummaryAsync(
        InternalUserBatchSummaryRequest request,
        Guid? viewerAuthUserId,
        CancellationToken cancellationToken = default)
    {
        var distinctUserIds = request.UserIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(100)
            .ToArray();

        if (distinctUserIds.Length == 0)
        {
            return new InternalUserBatchSummaryResponse([]);
        }

        var profiles = await userProfileRepository.ListByAuthUserIdsAsync(distinctUserIds, includeDeleted: false, cancellationToken);
        var viewerProfile = viewerAuthUserId.HasValue
            ? await userProfileRepository.GetByAuthUserIdAsync(viewerAuthUserId.Value, includeDeleted: false, cancellationToken)
            : null;
        var profileIds = profiles.Select(profile => profile.Id).ToArray();
        var blockedProfileIds = viewerProfile is null
            ? []
            : (await userBlockRepository.ListBlockedProfileIdsAsync(viewerProfile.Id, cancellationToken)).ToHashSet();
        var blockerProfileIds = viewerProfile is null
            ? []
            : (await userBlockRepository.ListBlockerProfileIdsAsync(viewerProfile.Id, cancellationToken)).ToHashSet();
        var followedProfileIds = viewerProfile is null
            ? []
            : (await userFollowRepository.ListFollowingProfileIdsAsync(viewerProfile.Id, profileIds, cancellationToken)).ToHashSet();
        var items = new List<InternalUserBatchSummaryItem>(profiles.Count);

        foreach (var profile in profiles.Where(static p => p.IsActive))
        {
            var isFollowedByCurrentUser = viewerProfile is null
                ? false
                : await userFollowRepository.ExistsActiveAsync(viewerProfile.Id, profile.Id, cancellationToken);
            var isFollowingCurrentUser = viewerProfile is null
                ? false
                : await userFollowRepository.ExistsActiveAsync(profile.Id, viewerProfile.Id, cancellationToken);
            var canViewPosts = CanViewPosts(
                profile,
                viewerAuthUserId,
                viewerProfile,
                blockedProfileIds,
                blockerProfileIds,
                followedProfileIds);

            items.Add(new InternalUserBatchSummaryItem(
                profile.AuthUserId,
                profile.Username,
                profile.DisplayName,
                profile.AvatarUrl,
                profile.IsVerified,
                canViewPosts,
                profile.ProfileVisibility.ToString(),
                profile.CommentAudience.ToString(),
                profile.HideLikeCounts,
                isFollowedByCurrentUser,
                isFollowingCurrentUser));
        }

        return new InternalUserBatchSummaryResponse(items);
    }

    public async Task<DiscoveryProfileExportResponse> GetDiscoveryProfileExportBatchAsync(
        DateTimeOffset? lastUpdatedAt,
        Guid? lastProfileId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 500);
        var profiles = await userProfileRepository.ListDiscoveryExportBatchAsync(
            lastUpdatedAt?.UtcDateTime,
            lastProfileId,
            safeTake,
            cancellationToken);

        var items = profiles.Select(profile => new DiscoveryProfileExportItem(
            profile.Id,
            profile.AuthUserId,
            profile.Username,
            profile.DisplayName,
            profile.AvatarUrl,
            profile.Bio,
            profile.IsVerified,
            profile.ProfileVisibility.ToString(),
            profile.IsActive,
            profile.IsDeleted,
            profile.FollowersCount,
            profile.PostsCount,
            ToDateTimeOffset(profile.UpdatedAt ?? profile.CreatedAt))).ToArray();

        var last = items.LastOrDefault();
        return new DiscoveryProfileExportResponse(
            items,
            last?.UpdatedAt,
            last?.ProfileId,
            items.Length == safeTake);
    }

    private async Task<(int FollowersCount, int FollowingCount)> LoadFollowCountsAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var followersCount = await userFollowRepository.CountFollowersAsync(profileId, cancellationToken);
        var followingCount = await userFollowRepository.CountFollowingAsync(profileId, cancellationToken);
        return (followersCount, followingCount);
    }

    private static MyProfileResponse MapMyProfile(
        UserProfile profile,
        (int FollowersCount, int FollowingCount) followCounts) => new(
        profile.Id,
        profile.AuthUserId,
        profile.Username,
        profile.NormalizedUsername,
        profile.DisplayName,
        profile.Bio,
        profile.AvatarUrl,
        profile.CoverPhotoUrl,
        profile.Country,
        profile.City,
        profile.District,
        profile.Occupation,
        profile.Company,
        profile.Website,
        followCounts.FollowersCount,
        followCounts.FollowingCount,
        profile.PostsCount,
        profile.IsVerified,
        profile.AccountType.ToString(),
        profile.ProfileVisibility.ToString(),
        profile.CommentAudience.ToString(),
        profile.HideLikeCounts,
        profile.LastSeenAt,
        profile.CreatedAt,
        profile.UpdatedAt);

    private async Task<ProfileRelationship> ResolveRelationshipAsync(
        UserProfile? viewerProfile,
        UserProfile profile,
        CancellationToken cancellationToken)
    {
        if (viewerProfile is null || viewerProfile.Id == profile.Id)
        {
            return new ProfileRelationship(false, false, false, false);
        }

        var isFollowedByCurrentUser = await userFollowRepository.ExistsActiveAsync(
            viewerProfile.Id,
            profile.Id,
            cancellationToken);
        var isFollowingCurrentUser = await userFollowRepository.ExistsActiveAsync(
            profile.Id,
            viewerProfile.Id,
            cancellationToken);
        var isFollowRequestPending = await userFollowRepository.ExistsPendingAsync(
            viewerProfile.Id,
            profile.Id,
            cancellationToken);
        var isProfileNotificationsEnabled = await profileNotificationSubscriptionRepository.ExistsAsync(
            viewerProfile.Id,
            profile.Id,
            cancellationToken);

        return new ProfileRelationship(
            isFollowedByCurrentUser,
            isFollowingCurrentUser,
            isFollowRequestPending,
            isProfileNotificationsEnabled);
    }

    private static bool CanViewPosts(
        UserProfile profile,
        Guid? viewerAuthUserId,
        UserProfile? viewerProfile,
        IReadOnlySet<Guid> blockedProfileIds,
        IReadOnlySet<Guid> blockerProfileIds,
        IReadOnlySet<Guid> followedProfileIds)
    {
        if (!profile.IsActive || profile.ProfileVisibility == Domain.Enums.ProfileVisibility.Hidden)
        {
            return false;
        }

        if (!viewerAuthUserId.HasValue)
        {
            return profile.ProfileVisibility == Domain.Enums.ProfileVisibility.Public;
        }

        if (viewerProfile is null)
        {
            return false;
        }

        if (viewerProfile.Id != profile.Id && (blockedProfileIds.Contains(profile.Id) || blockerProfileIds.Contains(profile.Id)))
        {
            return false;
        }

        return profile.ProfileVisibility switch
        {
            Domain.Enums.ProfileVisibility.Public => true,
            Domain.Enums.ProfileVisibility.Private => true,
            Domain.Enums.ProfileVisibility.FollowersOnly => viewerProfile.Id == profile.Id || followedProfileIds.Contains(profile.Id),
            _ => false
        };
    }

    private static ProfileResponse MapFullProfile(
        UserProfile profile,
        ProfileRelationship relationship,
        (int FollowersCount, int FollowingCount) followCounts) => new(
        profile.Id,
        profile.Username,
        profile.DisplayName,
        profile.Bio,
        profile.AvatarUrl,
        profile.CoverPhotoUrl,
        profile.Country,
        profile.City,
        profile.District,
        profile.Occupation,
        profile.Company,
        profile.Website,
        followCounts.FollowersCount,
        followCounts.FollowingCount,
        profile.PostsCount,
        profile.IsVerified,
        profile.AccountType.ToString(),
        profile.ProfileVisibility.ToString(),
        profile.CommentAudience.ToString(),
        profile.HideLikeCounts,
        profile.LastSeenAt,
        profile.CreatedAt,
        relationship.IsFollowedByCurrentUser,
        relationship.IsFollowingCurrentUser,
        relationship.IsFollowRequestPending,
        relationship.IsProfileNotificationsEnabled);

    private static ProfileResponse MapLimitedProfile(
        UserProfile profile,
        ProfileRelationship relationship,
        (int FollowersCount, int FollowingCount) followCounts) => new(
        profile.Id,
        profile.Username,
        profile.DisplayName,
        null,
        profile.AvatarUrl,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        followCounts.FollowersCount,
        followCounts.FollowingCount,
        profile.IsVerified,
        profile.AccountType.ToString(),
        profile.ProfileVisibility.ToString(),
        profile.CommentAudience.ToString(),
        profile.HideLikeCounts,
        null,
        null,
        relationship.IsFollowedByCurrentUser,
        relationship.IsFollowingCurrentUser,
        relationship.IsFollowRequestPending,
        relationship.IsProfileNotificationsEnabled);

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record ProfileRelationship(
        bool? IsFollowedByCurrentUser,
        bool? IsFollowingCurrentUser,
        bool? IsFollowRequestPending,
        bool? IsProfileNotificationsEnabled);
}
