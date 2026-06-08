// <copyright file="UserProfileReportService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;
using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Services;

public sealed class UserProfileReportService(
    IAccountsUnitOfWork unitOfWork,
    IUserProfileReportRepository reportRepository,
    IUserProfileRepository userProfileRepository,
    IUsernameNormalizer usernameNormalizer,
    IUsernameValidator usernameValidator) : IUserProfileReportService
{
    private static readonly HashSet<string> AllowedReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "Spam",
        "Harassment",
        "HateSpeech",
        "Nudity",
        "Violence",
        "Scam",
        "Other"
    };

    public async Task<UserProfileReportResult> ReportByUsernameAsync(
        Guid reporterAuthUserId,
        string reportedUsername,
        ReportUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (reporterAuthUserId == Guid.Empty)
        {
            return UserProfileReportResult.Unauthorized("authenticated_user_required");
        }

        var usernameValidation = usernameValidator.Validate(reportedUsername);
        if (!usernameValidation.IsValid)
        {
            return UserProfileReportResult.ValidationFailed(usernameValidation.Reason);
        }

        var reason = request.ReasonCode?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || !AllowedReasons.Contains(reason))
        {
            return UserProfileReportResult.ValidationFailed("report_reason_invalid");
        }

        var reporterProfile = await userProfileRepository.GetByAuthUserIdAsync(reporterAuthUserId, cancellationToken: cancellationToken);
        if (reporterProfile is null)
        {
            return UserProfileReportResult.ProvisioningPending("profile_provisioning_pending");
        }

        var normalizedUsername = usernameNormalizer.Normalize(reportedUsername);
        var reportedProfile = await userProfileRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken: cancellationToken);
        if (reportedProfile is null)
        {
            return UserProfileReportResult.NotFound("reported_profile_not_found");
        }

        if (reporterProfile.Id == reportedProfile.Id)
        {
            return UserProfileReportResult.ValidationFailed("cannot_report_self");
        }

        var report = new UserProfileReport
        {
            ReporterProfileId = reporterProfile.Id,
            ReportedProfileId = reportedProfile.Id,
            ReporterAuthUserId = reporterProfile.AuthUserId,
            ReportedAuthUserId = reportedProfile.AuthUserId,
            ReasonCode = reason,
            Description = NormalizeDescription(request.Description),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UserProfileReportResult.Success(new UserProfileReportResponse(report.Id, report.ReasonCode, report.CreatedAt));
    }

    private static string? NormalizeDescription(string? description)
    {
        var value = description?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= 2_000 ? value : value[..2_000];
    }
}
