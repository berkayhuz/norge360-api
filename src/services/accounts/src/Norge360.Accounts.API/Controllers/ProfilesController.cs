// <copyright file="ProfilesController.cs" company="Norge360">
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
[Route("api/accounts/profiles")]
public sealed class ProfilesController(
    IProfileAvatarUploadIntentService profileAvatarUploadIntentService,
    IProfileMutationService profileMutationService,
    IProfileQueryService profileQueryService,
    IProfileViewService profileViewService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MyProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        var result = await profileQueryService.GetMyProfileAsync(
            currentUserService.UserId,
            cancellationToken);

        return result.Status switch
        {
            ProfileQueryStatus.Success when result.Value is not null => Ok(result.Value),
            ProfileQueryStatus.ProvisioningPending => ProfileProvisioningPending(),
            ProfileQueryStatus.NotFound => ProfileProvisioningPending(),
            ProfileQueryStatus.Unauthorized => UnauthorizedProblem(),
            _ => ProfileProvisioningPending()
        };
    }

    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType<MyProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchMe(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        var result = await profileMutationService.UpdateMyProfileAsync(
            currentUserService.UserId,
            request,
            cancellationToken);

        return result.Status switch
        {
            UpdateMyProfileStatus.Success when result.Value is not null => Ok(result.Value),
            UpdateMyProfileStatus.ValidationFailed => ValidationProblem(result.Errors, result.ErrorCode),
            UpdateMyProfileStatus.NotFound => ProfileNotFound(),
            UpdateMyProfileStatus.Unauthorized => UnauthorizedProblem(),
            _ => Problem(
                title: "Profile update failed",
                detail: "The profile could not be updated.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("me/avatar/upload-intent")]
    [Authorize]
    [ProducesResponseType<AvatarUploadIntentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAvatarUploadIntent(
        [FromBody] CreateAvatarUploadIntentRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        var result = await profileAvatarUploadIntentService.CreateAsync(
            currentUserService.UserId,
            request,
            cancellationToken);

        return result.Status switch
        {
            CreateAvatarUploadIntentStatus.Success when result.Value is not null => Ok(result.Value),
            CreateAvatarUploadIntentStatus.ValidationFailed => ValidationProblem(result.Errors, result.ErrorCode),
            CreateAvatarUploadIntentStatus.Unauthorized => UnauthorizedProblem(),
            _ => Problem(
                title: "Avatar upload intent failed",
                detail: "The upload intent could not be created.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("me/avatar/complete")]
    [Authorize]
    [ProducesResponseType<MyProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteAvatarUpload(
        [FromBody] CompleteAvatarUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        var result = await profileMutationService.CompleteAvatarUploadAsync(
            currentUserService.UserId,
            request,
            cancellationToken);

        return result.Status switch
        {
            CompleteAvatarUploadStatus.Success when result.Value is not null => Ok(result.Value),
            CompleteAvatarUploadStatus.ValidationFailed => ValidationProblem(result.Errors, result.ErrorCode),
            CompleteAvatarUploadStatus.NotFound => ProfileNotFound(),
            CompleteAvatarUploadStatus.Unauthorized => UnauthorizedProblem(),
            _ => Problem(
                title: "Avatar completion failed",
                detail: "The avatar could not be linked to your profile.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{username}")]
    [AllowAnonymous]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUsername(string username, CancellationToken cancellationToken)
    {
        var viewerAuthUserId = currentUserService.IsAuthenticated
            ? currentUserService.UserId
            : (Guid?)null;

        var result = await profileQueryService.GetPublicProfileByUsernameAsync(
            username,
            viewerAuthUserId,
            cancellationToken);

        return result.Status switch
        {
            ProfileQueryStatus.Success when result.Value is not null => Ok(result.Value),
            _ => ProfileNotFound()
        };
    }

    [HttpPost("{username}/views")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TrackProfileView(string username, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            return UnauthorizedProblem();
        }

        var result = await profileViewService.TrackProfileViewAsync(
            currentUserService.UserId,
            username,
            cancellationToken);

        return result.Status switch
        {
            ProfileViewStatus.Accepted => Accepted(),
            ProfileViewStatus.ValidationFailed => ValidationProblem(result.ErrorCode),
            ProfileViewStatus.NotFound => ProfileNotFound(),
            ProfileViewStatus.ProvisioningPending => ProfileProvisioningPending(),
            _ => UnauthorizedProblem()
        };
    }

    private ObjectResult ProfileProvisioningPending() => Problem(
        title: "Profile provisioning pending",
        detail: "Your profile has not been provisioned yet. Please try again shortly.",
        statusCode: StatusCodes.Status202Accepted);

    private ObjectResult ProfileNotFound() => Problem(
        title: "Profile not found",
        detail: "The requested profile was not found.",
        statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ValidationProblem(
        IReadOnlyDictionary<string, string[]>? errors,
        string? errorCode)
    {
        var details = new ProblemDetails
        {
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest
        };

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            details.Extensions["errorCode"] = errorCode;
        }

        if (errors is not null && errors.Count > 0)
        {
            details.Extensions["errors"] = errors;
        }

        return StatusCode(StatusCodes.Status400BadRequest, details);
    }

    private ObjectResult UnauthorizedProblem() => Problem(
        title: "Unauthorized",
        detail: "Authentication is required to access this profile.",
        statusCode: StatusCodes.Status401Unauthorized);
}
