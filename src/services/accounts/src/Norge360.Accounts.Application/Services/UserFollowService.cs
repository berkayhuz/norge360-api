// <copyright file="UserFollowService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;

namespace Norge360.Accounts.Application.Services;

public sealed class UserFollowService(
    IAccountsUnitOfWork unitOfWork,
    IUserFollowRepository userFollowRepository,
    IUserBlockRepository userBlockRepository,
    IUserProfileRepository userProfileRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator,
    IDiscoveryEventPublisher discoveryEventPublisher,
    IAccountNotificationPublisher notificationPublisher,
    IProfileNotificationSubscriptionRepository profileNotificationSubscriptionRepository) : IUserFollowService
{
    public async Task<UserFollowMutationResult> FollowByUsernameAsync(
        Guid followerAuthUserId,
        string followeeUsername,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(followerAuthUserId, followeeUsername, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var followerProfile = context.FollowerProfile!;
        var followeeProfile = context.FolloweeProfile!;
        var existing = await userFollowRepository.GetAsync(followerProfile.Id, followeeProfile.Id, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == FollowStatus.Active)
            {
                return await BuildMutationResultAsync(followeeProfile.Id, true, false, cancellationToken);
            }

            if (followeeProfile.ProfileVisibility == ProfileVisibility.Private)
            {
                return await BuildMutationResultAsync(followeeProfile.Id, false, true, cancellationToken);
            }

            existing.Status = FollowStatus.Active;
            followerProfile.FollowingCount++;
            followeeProfile.FollowersCount++;
            await notificationPublisher.PublishFollowedAsync(followerProfile, followeeProfile, existing.Id, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await PublishFollowEventAsync("ProfileFollowed", existing, followerProfile, followeeProfile, cancellationToken);
            return await BuildMutationResultAsync(followeeProfile.Id, true, false, cancellationToken);
        }

        var requiresApproval = followeeProfile.ProfileVisibility == ProfileVisibility.Private;
        var follow = new UserFollow
        {
            FollowerId = followerProfile.Id,
            FolloweeId = followeeProfile.Id,
            Status = requiresApproval ? FollowStatus.Pending : FollowStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await userFollowRepository.AddAsync(follow, cancellationToken);
        if (requiresApproval)
        {
            await notificationPublisher.PublishFollowRequestAsync(followerProfile, followeeProfile, follow.Id, cancellationToken);
        }
        else
        {
            followerProfile.FollowingCount++;
            followeeProfile.FollowersCount++;
            await notificationPublisher.PublishFollowedAsync(followerProfile, followeeProfile, follow.Id, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!requiresApproval)
        {
            await PublishFollowEventAsync("ProfileFollowed", follow, followerProfile, followeeProfile, cancellationToken);
        }

        return await BuildMutationResultAsync(followeeProfile.Id, !requiresApproval, requiresApproval, cancellationToken);
    }

    public async Task<UserFollowMutationResult> UnfollowByUsernameAsync(
        Guid followerAuthUserId,
        string followeeUsername,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(followerAuthUserId, followeeUsername, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var followerProfile = context.FollowerProfile!;
        var followeeProfile = context.FolloweeProfile!;
        var existing = await userFollowRepository.GetAsync(followerProfile.Id, followeeProfile.Id, cancellationToken);
        if (existing is null)
        {
            return await BuildMutationResultAsync(followeeProfile.Id, false, false, cancellationToken);
        }

        var wasActive = existing.Status == FollowStatus.Active;
        userFollowRepository.Remove(existing);
        if (wasActive)
        {
            followerProfile.FollowingCount = Math.Max(0, followerProfile.FollowingCount - 1);
            followeeProfile.FollowersCount = Math.Max(0, followeeProfile.FollowersCount - 1);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasActive)
        {
            await PublishFollowEventAsync("ProfileUnfollowed", existing, followerProfile, followeeProfile, cancellationToken);
        }

        return await BuildMutationResultAsync(followeeProfile.Id, false, false, cancellationToken);
    }

    public async Task<UserFollowMutationResult> AcceptFollowRequestByUsernameAsync(
        Guid followeeAuthUserId,
        string followerUsername,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(followeeAuthUserId, followerUsername, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var followeeProfile = context.FollowerProfile!;
        var followerProfile = context.FolloweeProfile!;
        var existing = await userFollowRepository.GetAsync(followerProfile.Id, followeeProfile.Id, cancellationToken);
        if (existing is null)
        {
            return await BuildMutationResultAsync(followeeProfile.Id, false, false, cancellationToken);
        }

        if (existing.Status == FollowStatus.Active)
        {
            return await BuildMutationResultAsync(followeeProfile.Id, true, false, cancellationToken);
        }

        existing.Status = FollowStatus.Active;
        followerProfile.FollowingCount++;
        followeeProfile.FollowersCount++;
        await notificationPublisher.PublishFollowRequestAcceptedAsync(followerProfile, followeeProfile, existing.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishFollowEventAsync("ProfileFollowed", existing, followerProfile, followeeProfile, cancellationToken);
        return await BuildMutationResultAsync(followeeProfile.Id, true, false, cancellationToken);
    }

    public async Task<UserFollowMutationResult> RejectFollowRequestByUsernameAsync(
        Guid followeeAuthUserId,
        string followerUsername,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(followeeAuthUserId, followerUsername, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var followeeProfile = context.FollowerProfile!;
        var followerProfile = context.FolloweeProfile!;
        var existing = await userFollowRepository.GetAsync(followerProfile.Id, followeeProfile.Id, cancellationToken);
        if (existing is { Status: FollowStatus.Pending })
        {
            userFollowRepository.Remove(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await BuildMutationResultAsync(followeeProfile.Id, false, false, cancellationToken);
    }

    public async Task<UserFollowRelationResult> GetRelationByUsernameAsync(
        Guid requesterAuthUserId,
        string username,
        CancellationToken cancellationToken = default)
    {
        if (requesterAuthUserId == Guid.Empty)
        {
            return UserFollowRelationResult.Unauthorized("authenticated_user_required");
        }

        var validation = usernameValidator.Validate(username);
        if (!validation.IsValid)
        {
            return UserFollowRelationResult.ValidationFailed(validation.Reason);
        }

        var requesterProfile = await userProfileRepository.GetByAuthUserIdAsync(
            requesterAuthUserId,
            includeDeleted: false,
            cancellationToken);
        if (requesterProfile is null)
        {
            return UserFollowRelationResult.ProvisioningPending("profile_provisioning_pending");
        }

        var normalizedUsername = usernameNormalizer.Normalize(username);
        var targetProfile = await userProfileRepository.GetByNormalizedUsernameAsync(
            normalizedUsername,
            includeDeleted: false,
            cancellationToken);
        if (targetProfile is null || !targetProfile.IsActive || targetProfile.ProfileVisibility == ProfileVisibility.Hidden)
        {
            return UserFollowRelationResult.NotFound("profile_not_found");
        }

        var blocked = await userBlockRepository.ExistsBetweenAsync(
            requesterProfile.Id,
            targetProfile.Id,
            cancellationToken);
        if (blocked)
        {
            return UserFollowRelationResult.NotFound("profile_not_found");
        }

        var isSelf = requesterProfile.Id == targetProfile.Id;
        var isFollowing = !isSelf && await userFollowRepository.ExistsActiveAsync(requesterProfile.Id, targetProfile.Id, cancellationToken);
        var isFollowedBy = !isSelf && await userFollowRepository.ExistsActiveAsync(targetProfile.Id, requesterProfile.Id, cancellationToken);
        var isFollowRequestPending = !isSelf && await userFollowRepository.ExistsPendingAsync(requesterProfile.Id, targetProfile.Id, cancellationToken);
        var isProfileNotificationsEnabled = !isSelf && await profileNotificationSubscriptionRepository.ExistsAsync(requesterProfile.Id, targetProfile.Id, cancellationToken);
        var followersCount = await userFollowRepository.CountFollowersAsync(targetProfile.Id, cancellationToken);
        var followingCount = await userFollowRepository.CountFollowingAsync(targetProfile.Id, cancellationToken);

        return UserFollowRelationResult.Success(
            isFollowing,
            isFollowedBy,
            isFollowRequestPending,
            isProfileNotificationsEnabled,
            followersCount,
            followingCount);
    }

    public async Task<UserFollowListResult> ListFollowersByUsernameAsync(
        Guid? viewerAuthUserId,
        string username,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildListContextAsync(viewerAuthUserId, username, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (safePage - 1) * safePageSize;
        var items = await userFollowRepository.ListFollowersAsync(context.Profile!.Id, safePageSize, offset, cancellationToken);

        return UserFollowListResult.Success(safePage, safePageSize, items);
    }

    public async Task<UserFollowListResult> ListFollowingByUsernameAsync(
        Guid? viewerAuthUserId,
        string username,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildListContextAsync(viewerAuthUserId, username, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (safePage - 1) * safePageSize;
        var items = await userFollowRepository.ListFollowingAsync(context.Profile!.Id, safePageSize, offset, cancellationToken);

        return UserFollowListResult.Success(safePage, safePageSize, items);
    }

    private async Task<FollowContext> BuildContextAsync(
        Guid followerAuthUserId,
        string followeeUsername,
        CancellationToken cancellationToken)
    {
        if (followerAuthUserId == Guid.Empty)
        {
            return new FollowContext(null, null, UserFollowMutationResult.Unauthorized("authenticated_user_required"));
        }

        var validation = usernameValidator.Validate(followeeUsername);
        if (!validation.IsValid)
        {
            return new FollowContext(null, null, UserFollowMutationResult.ValidationFailed(validation.Reason));
        }

        var followerProfile = await userProfileRepository.GetTrackedByAuthUserIdAsync(followerAuthUserId, cancellationToken: cancellationToken);
        if (followerProfile is null)
        {
            return new FollowContext(null, null, UserFollowMutationResult.ProvisioningPending("profile_provisioning_pending"));
        }

        var normalizedUsername = usernameNormalizer.Normalize(followeeUsername);
        var followeeProfile = await userProfileRepository.GetTrackedByNormalizedUsernameAsync(normalizedUsername, cancellationToken: cancellationToken);
        if (followeeProfile is null || !followeeProfile.IsActive || followeeProfile.ProfileVisibility == ProfileVisibility.Hidden)
        {
            return new FollowContext(null, null, UserFollowMutationResult.NotFound("followee_profile_not_found"));
        }

        if (followeeProfile.Id == followerProfile.Id || followeeProfile.AuthUserId == followerAuthUserId)
        {
            return new FollowContext(null, null, UserFollowMutationResult.ValidationFailed("cannot_follow_self"));
        }

        var blocked = await userBlockRepository.ExistsBetweenAsync(
            followerProfile.Id,
            followeeProfile.Id,
            cancellationToken);
        if (blocked)
        {
            return new FollowContext(null, null, UserFollowMutationResult.ValidationFailed("follow_blocked_profile"));
        }

        return new FollowContext(followerProfile, followeeProfile, null);
    }

    private async Task<FollowListContext> BuildListContextAsync(
        Guid? viewerAuthUserId,
        string username,
        CancellationToken cancellationToken)
    {
        var value = username.Trim();
        var validation = usernameValidator.Validate(value);
        if (!validation.IsValid)
        {
            return new FollowListContext(null, UserFollowListResult.ValidationFailed(validation.Reason));
        }

        var normalizedUsername = usernameNormalizer.Normalize(value);
        var profile = await userProfileRepository.GetByNormalizedUsernameAsync(
            normalizedUsername,
            includeDeleted: false,
            cancellationToken);

        if (profile is null || !profile.IsActive)
        {
            return new FollowListContext(null, UserFollowListResult.NotFound("profile_not_found"));
        }

        UserProfile? viewerProfile = null;
        if (viewerAuthUserId.HasValue && viewerAuthUserId.Value != Guid.Empty)
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
                    return new FollowListContext(null, UserFollowListResult.NotFound("profile_not_found"));
                }
            }
        }

        var isSelf = viewerProfile is not null && viewerProfile.Id == profile.Id;
        if (profile.ProfileVisibility == ProfileVisibility.Private && !viewerAuthUserId.HasValue)
        {
            return new FollowListContext(null, UserFollowListResult.NotFound("profile_not_found"));
        }

        if (profile.ProfileVisibility == ProfileVisibility.FollowersOnly && !isSelf)
        {
            if (viewerProfile is null)
            {
                return new FollowListContext(null, UserFollowListResult.NotFound("profile_not_found"));
            }

            var canView = await userFollowRepository.ExistsActiveAsync(
                viewerProfile.Id,
                profile.Id,
                cancellationToken);
            if (!canView)
            {
                return new FollowListContext(null, UserFollowListResult.NotFound("profile_not_found"));
            }
        }

        if (profile.ProfileVisibility == ProfileVisibility.Hidden)
        {
            return new FollowListContext(null, UserFollowListResult.NotFound("profile_not_found"));
        }

        return new FollowListContext(profile, null);
    }

    private async Task<UserFollowMutationResult> BuildMutationResultAsync(
        Guid targetProfileId,
        bool isFollowing,
        bool isFollowRequestPending,
        CancellationToken cancellationToken)
    {
        var followersCount = await userFollowRepository.CountFollowersAsync(targetProfileId, cancellationToken);
        var followingCount = await userFollowRepository.CountFollowingAsync(targetProfileId, cancellationToken);
        return UserFollowMutationResult.Success(isFollowing, isFollowRequestPending, followersCount, followingCount);
    }

    private async Task PublishFollowEventAsync(
        string eventType,
        UserFollow follow,
        UserProfile followerProfile,
        UserProfile followeeProfile,
        CancellationToken cancellationToken)
    {
        try
        {
            await discoveryEventPublisher.PublishAsync(
                new DiscoveryEventEnvelope(
                    eventType,
                    "Accounts",
                    "UserFollow",
                    follow.Id.ToString("D"),
                    followerProfile.AuthUserId,
                    followerProfile.Id,
                    followeeProfile.AuthUserId,
                    followeeProfile.Id,
                    "UserProfile",
                    followeeProfile.Id.ToString("D"),
                    $"accounts:user-follow:{follow.Id:D}:{eventType}",
                    DateTime.UtcNow,
                    null),
                cancellationToken);
        }
        catch
        {
            // Discovery events are best-effort until a durable outbox publisher is wired.
        }
    }

    private sealed record FollowContext(UserProfile? FollowerProfile, UserProfile? FolloweeProfile, UserFollowMutationResult? Result);

    private sealed record FollowListContext(UserProfile? Profile, UserFollowListResult? Result);
}
