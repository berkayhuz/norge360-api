// <copyright file="ProfileViewResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record ProfileViewResult(ProfileViewStatus Status, string? ErrorCode = null)
{
    public static ProfileViewResult Accepted() => new(ProfileViewStatus.Accepted);

    public static ProfileViewResult Unauthorized(string errorCode) => new(ProfileViewStatus.Unauthorized, errorCode);

    public static ProfileViewResult ValidationFailed(string? errorCode) => new(ProfileViewStatus.ValidationFailed, errorCode);

    public static ProfileViewResult NotFound(string errorCode) => new(ProfileViewStatus.NotFound, errorCode);

    public static ProfileViewResult ProvisioningPending(string errorCode) => new(ProfileViewStatus.ProvisioningPending, errorCode);
}

public enum ProfileViewStatus
{
    Accepted = 0,
    Unauthorized = 1,
    ValidationFailed = 2,
    NotFound = 3,
    ProvisioningPending = 4
}
