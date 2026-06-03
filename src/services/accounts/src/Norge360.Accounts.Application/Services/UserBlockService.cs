// <copyright file="UserBlockService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Services;

public sealed class UserBlockService(
    IAccountsUnitOfWork unitOfWork,
    IUserBlockRepository userBlockRepository,
    IUserProfileRepository userProfileRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator) : IUserBlockService
{
    public async Task<UserBlockMutationResult> BlockByUsernameAsync(
        Guid blockerAuthUserId,
        string blockedUsername,
        CancellationToken cancellationToken = default)
    {
        if (blockerAuthUserId == Guid.Empty)
        {
            return UserBlockMutationResult.Unauthorized("authenticated_user_required");
        }

        var validation = usernameValidator.Validate(blockedUsername);
        if (!validation.IsValid)
        {
            return UserBlockMutationResult.ValidationFailed(validation.Reason);
        }

        var blockerProfile = await userProfileRepository.GetByAuthUserIdAsync(blockerAuthUserId, cancellationToken: cancellationToken);
        if (blockerProfile is null)
        {
            return UserBlockMutationResult.ProvisioningPending("profile_provisioning_pending");
        }

        var normalizedUsername = usernameNormalizer.Normalize(blockedUsername);
        var blockedProfile = await userProfileRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken: cancellationToken);
        if (blockedProfile is null)
        {
            return UserBlockMutationResult.NotFound("blocked_profile_not_found");
        }

        if (blockedProfile.Id == blockerProfile.Id)
        {
            return UserBlockMutationResult.ValidationFailed("cannot_block_self");
        }

        var existing = await userBlockRepository.GetAsync(blockerProfile.Id, blockedProfile.Id, cancellationToken);
        if (existing is not null)
        {
            return UserBlockMutationResult.Success();
        }

        await userBlockRepository.AddAsync(
            new UserBlock
            {
                BlockerProfileId = blockerProfile.Id,
                BlockedProfileId = blockedProfile.Id,
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserBlockMutationResult.Success();
    }

    public async Task<UserBlockMutationResult> UnblockByUsernameAsync(
        Guid blockerAuthUserId,
        string blockedUsername,
        CancellationToken cancellationToken = default)
    {
        if (blockerAuthUserId == Guid.Empty)
        {
            return UserBlockMutationResult.Unauthorized("authenticated_user_required");
        }

        var validation = usernameValidator.Validate(blockedUsername);
        if (!validation.IsValid)
        {
            return UserBlockMutationResult.ValidationFailed(validation.Reason);
        }

        var blockerProfile = await userProfileRepository.GetByAuthUserIdAsync(blockerAuthUserId, cancellationToken: cancellationToken);
        if (blockerProfile is null)
        {
            return UserBlockMutationResult.ProvisioningPending("profile_provisioning_pending");
        }

        var normalizedUsername = usernameNormalizer.Normalize(blockedUsername);
        var blockedProfile = await userProfileRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken: cancellationToken);
        if (blockedProfile is null)
        {
            return UserBlockMutationResult.NotFound("blocked_profile_not_found");
        }

        var existing = await userBlockRepository.GetAsync(blockerProfile.Id, blockedProfile.Id, cancellationToken);
        if (existing is null)
        {
            return UserBlockMutationResult.Success();
        }

        userBlockRepository.Remove(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserBlockMutationResult.Success();
    }

    public async Task<UserBlockListResult> ListBlockedAsync(
        Guid blockerAuthUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (blockerAuthUserId == Guid.Empty)
        {
            return UserBlockListResult.Unauthorized("authenticated_user_required");
        }

        var blockerProfile = await userProfileRepository.GetByAuthUserIdAsync(blockerAuthUserId, cancellationToken: cancellationToken);
        if (blockerProfile is null)
        {
            return UserBlockListResult.ProvisioningPending("profile_provisioning_pending");
        }

        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (safePage - 1) * safePageSize;
        var items = await userBlockRepository.ListBlockedAsync(blockerProfile.Id, safePageSize, offset, cancellationToken);

        return UserBlockListResult.Success(safePage, safePageSize, items);
    }

    public async Task<UserBlockRelationsResult> ListBlockRelationsAsync(
        Guid requesterAuthUserId,
        CancellationToken cancellationToken = default)
    {
        if (requesterAuthUserId == Guid.Empty)
        {
            return UserBlockRelationsResult.Unauthorized("authenticated_user_required");
        }

        var requesterProfile = await userProfileRepository.GetByAuthUserIdAsync(requesterAuthUserId, cancellationToken: cancellationToken);
        if (requesterProfile is null)
        {
            return UserBlockRelationsResult.ProvisioningPending("profile_provisioning_pending");
        }

        var blocked = await userBlockRepository.ListBlockedProfileIdsAsync(requesterProfile.Id, cancellationToken);
        var blockers = await userBlockRepository.ListBlockerProfileIdsAsync(requesterProfile.Id, cancellationToken);

        var existingBlocked = await userProfileRepository.ListExistingProfileIdsAsync(blocked, cancellationToken);
        var existingBlockers = await userProfileRepository.ListExistingProfileIdsAsync(blockers, cancellationToken);

        return UserBlockRelationsResult.Success(existingBlocked, existingBlockers);
    }
}
