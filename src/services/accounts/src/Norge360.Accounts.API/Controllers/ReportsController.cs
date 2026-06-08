// <copyright file="ReportsController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;
using Norge360.CurrentUser;

namespace Norge360.Accounts.API.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts/reports")]
public sealed class ReportsController(
    IUserProfileReportService reportService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("{username}")]
    [ProducesResponseType<UserProfileReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportByUsername(
        string username,
        [FromBody] ReportUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reportService.ReportByUsernameAsync(GetUserIdOrEmpty(), username, request, cancellationToken);
        return result.Status switch
        {
            UserProfileReportStatus.Success => Ok(result.Response),
            UserProfileReportStatus.ValidationFailed => ProblemResult(StatusCodes.Status400BadRequest, "Validation failed", result.ErrorCode ?? "report_invalid"),
            UserProfileReportStatus.NotFound => ProblemResult(StatusCodes.Status404NotFound, "Profile not found", result.ErrorCode ?? "profile_not_found"),
            UserProfileReportStatus.ProvisioningPending => ProblemResult(StatusCodes.Status202Accepted, "Profile provisioning pending", result.ErrorCode ?? "profile_provisioning_pending"),
            _ => ProblemResult(StatusCodes.Status401Unauthorized, "Unauthorized", result.ErrorCode ?? "authenticated_user_required")
        };
    }

    private Guid GetUserIdOrEmpty() =>
        currentUserService.IsAuthenticated && currentUserService.UserId != Guid.Empty
            ? currentUserService.UserId
            : Guid.Empty;

    private ObjectResult ProblemResult(int statusCode, string title, string errorCode)
    {
        var details = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = errorCode
        };
        details.Extensions["errorCode"] = errorCode;
        return StatusCode(statusCode, details);
    }
}
