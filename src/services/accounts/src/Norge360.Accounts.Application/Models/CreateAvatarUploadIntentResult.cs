// <copyright file="CreateAvatarUploadIntentResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Models;

public sealed record CreateAvatarUploadIntentResult(
    CreateAvatarUploadIntentStatus Status,
    AvatarUploadIntentResponse? Value = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? ErrorCode = null)
{
    public static CreateAvatarUploadIntentResult Success(AvatarUploadIntentResponse value) =>
        new(CreateAvatarUploadIntentStatus.Success, value);

    public static CreateAvatarUploadIntentResult ValidationFailed(
        IReadOnlyDictionary<string, string[]> errors,
        string? errorCode = null) =>
        new(CreateAvatarUploadIntentStatus.ValidationFailed, Errors: errors, ErrorCode: errorCode);

    public static CreateAvatarUploadIntentResult Unauthorized(string? errorCode = null) =>
        new(CreateAvatarUploadIntentStatus.Unauthorized, ErrorCode: errorCode);

    public static CreateAvatarUploadIntentResult Failed(string? errorCode = null) =>
        new(CreateAvatarUploadIntentStatus.Failed, ErrorCode: errorCode);
}

public enum CreateAvatarUploadIntentStatus
{
    Success = 0,
    ValidationFailed = 1,
    Unauthorized = 2,
    Failed = 3
}

