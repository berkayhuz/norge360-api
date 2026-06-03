// <copyright file="UpdateMyProfileResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Models;

public sealed record UpdateMyProfileResult(
    UpdateMyProfileStatus Status,
    MyProfileResponse? Value = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? ErrorCode = null)
{
    public static UpdateMyProfileResult Success(MyProfileResponse response) =>
        new(UpdateMyProfileStatus.Success, response);

    public static UpdateMyProfileResult Unauthorized(string? errorCode = null) =>
        new(UpdateMyProfileStatus.Unauthorized, ErrorCode: errorCode);

    public static UpdateMyProfileResult NotFound(string? errorCode = null) =>
        new(UpdateMyProfileStatus.NotFound, ErrorCode: errorCode);

    public static UpdateMyProfileResult ValidationFailed(
        IReadOnlyDictionary<string, string[]> errors,
        string? errorCode = null) =>
        new(UpdateMyProfileStatus.ValidationFailed, Errors: errors, ErrorCode: errorCode);
}

public enum UpdateMyProfileStatus
{
    Success = 0,
    ValidationFailed = 1,
    Unauthorized = 2,
    NotFound = 3
}
