// <copyright file="ProfileNotificationSubscriptionResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public sealed record ProfileNotificationSubscriptionResult(
    ProfileNotificationSubscriptionStatus Status,
    bool IsSubscribed,
    string? ErrorCode = null)
{
    public static ProfileNotificationSubscriptionResult Success(bool isSubscribed) =>
        new(ProfileNotificationSubscriptionStatus.Success, isSubscribed);

    public static ProfileNotificationSubscriptionResult Unauthorized(string errorCode) =>
        new(ProfileNotificationSubscriptionStatus.Unauthorized, false, errorCode);

    public static ProfileNotificationSubscriptionResult ValidationFailed(string? errorCode) =>
        new(ProfileNotificationSubscriptionStatus.ValidationFailed, false, errorCode);

    public static ProfileNotificationSubscriptionResult NotFound(string errorCode) =>
        new(ProfileNotificationSubscriptionStatus.NotFound, false, errorCode);

    public static ProfileNotificationSubscriptionResult ProvisioningPending(string errorCode) =>
        new(ProfileNotificationSubscriptionStatus.ProvisioningPending, false, errorCode);
}

public enum ProfileNotificationSubscriptionStatus
{
    Success = 0,
    Unauthorized = 1,
    ValidationFailed = 2,
    NotFound = 3,
    ProvisioningPending = 4
}
