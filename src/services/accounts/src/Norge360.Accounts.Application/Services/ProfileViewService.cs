// <copyright file="ProfileViewService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileViewService(
    IUserProfileRepository userProfileRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator,
    IDiscoveryEventPublisher discoveryEventPublisher) : IProfileViewService
{
    public async Task<ProfileViewResult> TrackProfileViewAsync(
        Guid viewerAuthUserId,
        string viewedUsername,
        CancellationToken cancellationToken = default)
    {
        if (viewerAuthUserId == Guid.Empty)
        {
            return ProfileViewResult.Unauthorized("authenticated_user_required");
        }

        var validation = usernameValidator.Validate(viewedUsername);
        if (!validation.IsValid)
        {
            return ProfileViewResult.ValidationFailed(validation.Reason);
        }

        var viewerProfile = await userProfileRepository.GetByAuthUserIdAsync(viewerAuthUserId, cancellationToken: cancellationToken);
        if (viewerProfile is null)
        {
            return ProfileViewResult.ProvisioningPending("profile_provisioning_pending");
        }

        var normalizedUsername = usernameNormalizer.Normalize(viewedUsername);
        var viewedProfile = await userProfileRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken: cancellationToken);
        if (viewedProfile is null)
        {
            return ProfileViewResult.NotFound("viewed_profile_not_found");
        }

        if (viewerProfile.Id == viewedProfile.Id || viewedProfile.AuthUserId == viewerAuthUserId)
        {
            return ProfileViewResult.Accepted();
        }

        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            await discoveryEventPublisher.PublishAsync(
                new DiscoveryEventEnvelope(
                    "ProfileViewed",
                    "Accounts",
                    "UserProfileView",
                    $"{viewerProfile.Id:D}:{viewedProfile.Id:D}:{day:yyyyMMdd}",
                    viewerProfile.AuthUserId,
                    viewerProfile.Id,
                    viewedProfile.AuthUserId,
                    viewedProfile.Id,
                    "UserProfile",
                    viewedProfile.Id.ToString("D"),
                    $"accounts:profile-view:{viewerProfile.Id:D}:{viewedProfile.Id:D}:{day:yyyyMMdd}",
                    DateTime.UtcNow,
                    null),
                cancellationToken);
        }
        catch
        {
            // Discovery view events are best-effort and must not break profile reads.
        }

        return ProfileViewResult.Accepted();
    }
}
