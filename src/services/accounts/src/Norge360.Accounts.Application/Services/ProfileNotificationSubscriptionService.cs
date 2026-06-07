// <copyright file="ProfileNotificationSubscriptionService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileNotificationSubscriptionService(
    IAccountsUnitOfWork unitOfWork,
    IProfileNotificationSubscriptionRepository subscriptionRepository,
    IUserBlockRepository userBlockRepository,
    IUserProfileRepository userProfileRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator) : IProfileNotificationSubscriptionService
{
    public async Task<ProfileNotificationSubscriptionResult> GetByUsernameAsync(
        Guid subscriberAuthUserId,
        string username,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(subscriberAuthUserId, username, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var exists = await subscriptionRepository.ExistsAsync(
            context.SubscriberProfile!.Id,
            context.TargetProfile!.Id,
            cancellationToken);
        return ProfileNotificationSubscriptionResult.Success(exists);
    }

    public async Task<ProfileNotificationSubscriptionResult> SubscribeByUsernameAsync(
        Guid subscriberAuthUserId,
        string username,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(subscriberAuthUserId, username, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var existing = await subscriptionRepository.GetAsync(
            context.SubscriberProfile!.Id,
            context.TargetProfile!.Id,
            cancellationToken);
        if (existing is null)
        {
            await subscriptionRepository.AddAsync(
                new UserProfileNotificationSubscription
                {
                    SubscriberProfileId = context.SubscriberProfile.Id,
                    TargetProfileId = context.TargetProfile.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ProfileNotificationSubscriptionResult.Success(true);
    }

    public async Task<ProfileNotificationSubscriptionResult> UnsubscribeByUsernameAsync(
        Guid subscriberAuthUserId,
        string username,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(subscriberAuthUserId, username, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        var existing = await subscriptionRepository.GetAsync(
            context.SubscriberProfile!.Id,
            context.TargetProfile!.Id,
            cancellationToken);
        if (existing is not null)
        {
            subscriptionRepository.Remove(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ProfileNotificationSubscriptionResult.Success(false);
    }

    private async Task<SubscriptionContext> BuildContextAsync(
        Guid subscriberAuthUserId,
        string username,
        CancellationToken cancellationToken)
    {
        if (subscriberAuthUserId == Guid.Empty)
        {
            return new SubscriptionContext(null, null, ProfileNotificationSubscriptionResult.Unauthorized("authenticated_user_required"));
        }

        var validation = usernameValidator.Validate(username);
        if (!validation.IsValid)
        {
            return new SubscriptionContext(null, null, ProfileNotificationSubscriptionResult.ValidationFailed(validation.Reason));
        }

        var subscriberProfile = await userProfileRepository.GetByAuthUserIdAsync(subscriberAuthUserId, includeDeleted: false, cancellationToken);
        if (subscriberProfile is null)
        {
            return new SubscriptionContext(null, null, ProfileNotificationSubscriptionResult.ProvisioningPending("profile_provisioning_pending"));
        }

        var normalizedUsername = usernameNormalizer.Normalize(username);
        var targetProfile = await userProfileRepository.GetByNormalizedUsernameAsync(normalizedUsername, includeDeleted: false, cancellationToken);
        if (targetProfile is null || !targetProfile.IsActive || targetProfile.ProfileVisibility == ProfileVisibility.Hidden)
        {
            return new SubscriptionContext(null, null, ProfileNotificationSubscriptionResult.NotFound("profile_not_found"));
        }

        if (targetProfile.Id == subscriberProfile.Id)
        {
            return new SubscriptionContext(null, null, ProfileNotificationSubscriptionResult.ValidationFailed("cannot_subscribe_self"));
        }

        var blocked = await userBlockRepository.ExistsBetweenAsync(subscriberProfile.Id, targetProfile.Id, cancellationToken);
        if (blocked)
        {
            return new SubscriptionContext(null, null, ProfileNotificationSubscriptionResult.NotFound("profile_not_found"));
        }

        return new SubscriptionContext(subscriberProfile, targetProfile, null);
    }

    private sealed record SubscriptionContext(
        UserProfile? SubscriberProfile,
        UserProfile? TargetProfile,
        ProfileNotificationSubscriptionResult? Result);
}
