// <copyright file="UserProfileReportResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Models;

public enum UserProfileReportStatus
{
    Success,
    Unauthorized,
    ValidationFailed,
    NotFound,
    ProvisioningPending
}

public sealed record UserProfileReportResult(
    UserProfileReportStatus Status,
    UserProfileReportResponse? Response = null,
    string? ErrorCode = null)
{
    public static UserProfileReportResult Success(UserProfileReportResponse response) => new(UserProfileReportStatus.Success, response);
    public static UserProfileReportResult Unauthorized(string errorCode) => new(UserProfileReportStatus.Unauthorized, ErrorCode: errorCode);
    public static UserProfileReportResult ValidationFailed(string? errorCode) => new(UserProfileReportStatus.ValidationFailed, ErrorCode: errorCode);
    public static UserProfileReportResult NotFound(string errorCode) => new(UserProfileReportStatus.NotFound, ErrorCode: errorCode);
    public static UserProfileReportResult ProvisioningPending(string errorCode) => new(UserProfileReportStatus.ProvisioningPending, ErrorCode: errorCode);
}
