// <copyright file="IProfileVisibilityPolicy.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IProfileVisibilityPolicy
{
    ProfileVisibilityDecision Evaluate(UserProfile profile, Guid? viewerAuthUserId);
}

public enum ProfileVisibilityDecision
{
    Full = 0,
    Limited = 1,
    NotFound = 2
}
