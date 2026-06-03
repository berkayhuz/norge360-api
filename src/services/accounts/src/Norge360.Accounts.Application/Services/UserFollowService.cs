// <copyright file="UserFollowService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Services;

public sealed class UserFollowService(
    IAccountsUnitOfWork unitOfWork,
    IUserFollowRepository userFollowRepository,
    IUserProfileRepository userProfileRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator,
    IDiscoveryEventPublisher discoveryEventPublisher) : IUserFollowService
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

        var existing = await userFollowRepository.GetAsync(context.FollowerProfile!.Id, context.FolloweeProfile!.Id, cancellationToken);
        if (existing is not null)
        {
            return UserFollowMutationResult.Success();
        }

        var follow = new UserFollow
        {
            FollowerId = context.FollowerProfile.Id,
            FolloweeId = context.FolloweeProfile.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await userFollowRepository.AddAsync(follow, cancellationToken);
        context.FollowerProfile.FollowingCount++;
        context.FolloweeProfile.FollowersCount++;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishFollowEventAsync("ProfileFollowed", follow, context.FollowerProfile, context.FolloweeProfile, cancellationToken);
        return UserFollowMutationResult.Success();
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

        var existing = await userFollowRepository.GetAsync(context.FollowerProfile!.Id, context.FolloweeProfile!.Id, cancellationToken);
        if (existing is null)
        {
            return UserFollowMutationResult.Success();
        }

        userFollowRepository.Remove(existing);
        context.FollowerProfile.FollowingCount = Math.Max(0, context.FollowerProfile.FollowingCount - 1);
        context.FolloweeProfile.FollowersCount = Math.Max(0, context.FolloweeProfile.FollowersCount - 1);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishFollowEventAsync("ProfileUnfollowed", existing, context.FollowerProfile, context.FolloweeProfile, cancellationToken);
        return UserFollowMutationResult.Success();
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
        if (followeeProfile is null)
        {
            return new FollowContext(null, null, UserFollowMutationResult.NotFound("followee_profile_not_found"));
        }

        if (followeeProfile.Id == followerProfile.Id || followeeProfile.AuthUserId == followerAuthUserId)
        {
            return new FollowContext(null, null, UserFollowMutationResult.ValidationFailed("cannot_follow_self"));
        }

        return new FollowContext(followerProfile, followeeProfile, null);
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
}
