// <copyright file="UserBlockMutationResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public enum UserBlockMutationStatus
{
    Success = 0,
    Unauthorized = 1,
    ProvisioningPending = 2,
    NotFound = 3,
    ValidationFailed = 4
}

public sealed record UserBlockMutationResult(
    UserBlockMutationStatus Status,
    string? ErrorCode = null)
{
    public static UserBlockMutationResult Success() => new(UserBlockMutationStatus.Success);
    public static UserBlockMutationResult Unauthorized(string? errorCode = null) => new(UserBlockMutationStatus.Unauthorized, errorCode);
    public static UserBlockMutationResult ProvisioningPending(string? errorCode = null) => new(UserBlockMutationStatus.ProvisioningPending, errorCode);
    public static UserBlockMutationResult NotFound(string? errorCode = null) => new(UserBlockMutationStatus.NotFound, errorCode);
    public static UserBlockMutationResult ValidationFailed(string? errorCode = null) => new(UserBlockMutationStatus.ValidationFailed, errorCode);
}
