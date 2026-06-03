// <copyright file="CompleteAvatarUploadResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Models;

public sealed record CompleteAvatarUploadResult(
    CompleteAvatarUploadStatus Status,
    MyProfileResponse? Value = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? ErrorCode = null)
{
    public static CompleteAvatarUploadResult Success(MyProfileResponse response) =>
        new(CompleteAvatarUploadStatus.Success, response);

    public static CompleteAvatarUploadResult Unauthorized(string? errorCode = null) =>
        new(CompleteAvatarUploadStatus.Unauthorized, ErrorCode: errorCode);

    public static CompleteAvatarUploadResult NotFound(string? errorCode = null) =>
        new(CompleteAvatarUploadStatus.NotFound, ErrorCode: errorCode);

    public static CompleteAvatarUploadResult ValidationFailed(
        IReadOnlyDictionary<string, string[]> errors,
        string? errorCode = null) =>
        new(CompleteAvatarUploadStatus.ValidationFailed, Errors: errors, ErrorCode: errorCode);

    public static CompleteAvatarUploadResult Failed(string? errorCode = null) =>
        new(CompleteAvatarUploadStatus.Failed, ErrorCode: errorCode);
}

public enum CompleteAvatarUploadStatus
{
    Success = 0,
    ValidationFailed = 1,
    Unauthorized = 2,
    NotFound = 3,
    Failed = 4
}

