// <copyright file="ProfileVisibilityPolicy.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileVisibilityPolicy : IProfileVisibilityPolicy
{
    public ProfileVisibilityDecision Evaluate(UserProfile profile, Guid? viewerAuthUserId)
    {
        if (viewerAuthUserId.HasValue && profile.AuthUserId == viewerAuthUserId.Value)
        {
            return ProfileVisibilityDecision.Full;
        }

        return profile.ProfileVisibility switch
        {
            ProfileVisibility.Public => ProfileVisibilityDecision.Full,
            ProfileVisibility.Private => viewerAuthUserId.HasValue ? ProfileVisibilityDecision.Full : ProfileVisibilityDecision.NotFound,
            ProfileVisibility.FollowersOnly => viewerAuthUserId.HasValue ? ProfileVisibilityDecision.Full : ProfileVisibilityDecision.NotFound,
            ProfileVisibility.Hidden => ProfileVisibilityDecision.NotFound,
            _ => ProfileVisibilityDecision.NotFound
        };
    }
}
