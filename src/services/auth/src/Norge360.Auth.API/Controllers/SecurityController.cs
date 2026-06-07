// <copyright file="SecurityController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norge360.Auth.API.Accessors;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Contracts.Internal;
using Norge360.Auth.Domain.Entities;
using Norge360.Clock;

namespace Norge360.Auth.API.Controllers;

[ApiController]
[Authorize]
[Route("api/auth/security")]
public sealed class SecurityController(
    AuthRequestContextAccessor requestContextAccessor,
    IUserRepository userRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IUserMfaRecoveryCodeRepository recoveryCodeRepository,
    IAuthenticatorKeyProtector authenticatorKeyProtector,
    IAuthenticatorTotpService authenticatorTotpService,
    IRecoveryCodeService recoveryCodeService,
    IAuthUnitOfWork unitOfWork,
    IClock clock) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType<AccountSecuritySummaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var trustedDevices = await trustedDeviceRepository.ListForUserAsync(principal.UserId, cancellationToken);
        var recoveryCodesRemaining = await recoveryCodeRepository.CountActiveAsync(principal.UserId, cancellationToken);
        var lastSecurityEventAt = new[]
        {
            user.PasswordChangedAt,
            user.LastLoginAt,
            user.EmailConfirmedAt,
            user.MfaEnabledAt,
            user.RecoveryCodesGeneratedAt,
            user.UpdatedAt
        }
        .Where(value => value.HasValue)
        .Select(value => value!.Value)
        .DefaultIfEmpty()
        .Max();

        return Ok(new AccountSecuritySummaryResponse(
            Email: user.Email ?? principal.Email ?? string.Empty,
            EmailConfirmed: user.EmailConfirmed,
            PasswordChangedAt: ToOffset(user.PasswordChangedAt),
            LastLoginAt: ToOffset(user.LastLoginAt),
            IsMfaEnabled: user.MfaEnabled,
            HasAuthenticator: !string.IsNullOrWhiteSpace(user.AuthenticatorKeyProtected),
            RecoveryCodesRemaining: recoveryCodesRemaining,
            TrustedDevicesCount: trustedDevices.Count(device => !device.IsRevoked),
            LastSecurityEventAt: ToOffset(lastSecurityEventAt)));
    }

    [HttpGet("mfa")]
    [ProducesResponseType<MfaStatusResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMfaStatus(CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var recoveryCodesRemaining = await recoveryCodeRepository.CountActiveAsync(principal.UserId, cancellationToken);
        return Ok(new MfaStatusResult(
            IsEnabled: user.MfaEnabled,
            HasAuthenticator: !string.IsNullOrWhiteSpace(user.AuthenticatorKeyProtected),
            RecoveryCodesRemaining: recoveryCodesRemaining));
    }

    [HttpPost("mfa/setup")]
    [ProducesResponseType<MfaSetupResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var sharedKey = authenticatorTotpService.GenerateSharedKey();
        var accountName = user.Email ?? principal.Email ?? $"user-{user.Id:N}";
        return Ok(new MfaSetupResult(
            SharedKey: sharedKey,
            AuthenticatorUri: authenticatorTotpService.BuildAuthenticatorUri("Norge360", accountName, sharedKey)));
    }

    [HttpPost("mfa/confirm")]
    [ProducesResponseType<MfaConfirmResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmMfa(
        [FromBody] MfaSetupConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var sharedKey = request.SharedKey?.Trim() ?? string.Empty;
        var verificationCode = request.VerificationCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sharedKey) || string.IsNullOrWhiteSpace(verificationCode))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["sharedKey"] = ["Shared key is required."],
                    ["verificationCode"] = ["Verification code is required."]
                },
                "mfa_setup_invalid");
        }

        var utcNow = clock.UtcDateTime;
        if (!authenticatorTotpService.VerifyCode(sharedKey, verificationCode, utcNow))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["verificationCode"] = ["The authenticator code is invalid."]
                },
                "invalid_mfa_code");
        }

        user.AuthenticatorKeyProtected = authenticatorKeyProtector.Protect(sharedKey);
        user.AuthenticatorKeyCreatedAt = utcNow;
        user.AuthenticatorKeyConfirmedAt = utcNow;
        user.MfaEnabled = true;
        user.MfaEnabledAt = utcNow;
        user.RecoveryCodesGeneratedAt = utcNow;

        var recoveryCodes = recoveryCodeService.GenerateCodes(10);
        await recoveryCodeRepository.ReplaceActiveAsync(
            user.Id,
            recoveryCodes
                .Select(code => new UserMfaRecoveryCode
                {
                    UserId = user.Id,
                    CodeHash = recoveryCodeService.HashCode(user.Id, code),
                    CreatedAt = utcNow
                })
                .ToArray(),
            utcNow,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new MfaConfirmResult(true, recoveryCodes));
    }

    [HttpPost("mfa/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableMfa([FromBody] MfaDisableRequest request, CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var verificationCode = request.VerificationCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["verificationCode"] = ["Verification code is required."]
                },
                "invalid_mfa_code");
        }

        var utcNow = clock.UtcDateTime;
        var verified = await ValidateCurrentSecurityCodeAsync(user, verificationCode, utcNow, cancellationToken);
        if (!verified)
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["verificationCode"] = ["The verification code is invalid."]
                },
                "invalid_mfa_code");
        }

        user.MfaEnabled = false;
        user.MfaEnabledAt = null;
        user.AuthenticatorKeyProtected = null;
        user.AuthenticatorKeyCreatedAt = null;
        user.AuthenticatorKeyConfirmedAt = null;
        user.RecoveryCodesGeneratedAt = null;

        await recoveryCodeRepository.RevokeActiveAsync(user.Id, utcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("mfa/recovery-codes")]
    [ProducesResponseType<RecoveryCodesResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegenerateRecoveryCodes([FromBody] MfaDisableRequest request, CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        if (!user.MfaEnabled || string.IsNullOrWhiteSpace(user.AuthenticatorKeyProtected))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["mfa"] = ["Enable two-factor authentication first."]
                },
                "mfa_not_enabled");
        }

        var verificationCode = request.VerificationCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["verificationCode"] = ["Verification code is required."]
                },
                "invalid_mfa_code");
        }

        var utcNow = clock.UtcDateTime;
        var verified = await ValidateCurrentSecurityCodeAsync(user, verificationCode, utcNow, cancellationToken);
        if (!verified)
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["verificationCode"] = ["The verification code is invalid."]
                },
                "invalid_mfa_code");
        }

        var recoveryCodes = recoveryCodeService.GenerateCodes(10);
        await recoveryCodeRepository.ReplaceActiveAsync(
            user.Id,
            recoveryCodes
                .Select(code => new UserMfaRecoveryCode
                {
                    UserId = user.Id,
                    CodeHash = recoveryCodeService.HashCode(user.Id, code),
                    CreatedAt = utcNow
                })
                .ToArray(),
            utcNow,
            cancellationToken);

        user.RecoveryCodesGeneratedAt = utcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new RecoveryCodesResult(recoveryCodes));
    }

    [HttpGet("trusted-devices")]
    [ProducesResponseType<TrustedDeviceSummaryResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTrustedDevices(CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var devices = await trustedDeviceRepository.ListForUserAsync(principal.UserId, cancellationToken);
        return Ok(devices.Select(device => new TrustedDeviceSummaryResponse(
            device.Id,
            IsCurrent: false,
            device.DeviceName,
            device.IpAddress,
            device.UserAgent,
            device.TrustedAtUtc,
            device.LastSeenAtUtc,
            device.IsRevoked)).ToArray());
    }

    [HttpDelete("trusted-devices/{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeTrustedDevice([FromRoute] Guid deviceId, CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var revoked = await trustedDeviceRepository.RevokeAsync(
            principal.UserId,
            deviceId,
            clock.UtcDateTime,
            "revoked_by_user",
            cancellationToken);

        if (!revoked)
        {
            return NotFoundProblem("Trusted device not found.", "The trusted device could not be found.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> ValidateCurrentSecurityCodeAsync(
        User user,
        string verificationCode,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(user.AuthenticatorKeyProtected))
        {
            var authenticatorCode = verificationCode.Length == 6 && verificationCode.All(char.IsDigit);
            if (authenticatorCode)
            {
                var sharedKey = authenticatorKeyProtector.Unprotect(user.AuthenticatorKeyProtected);
                if (authenticatorTotpService.VerifyCode(sharedKey, verificationCode, utcNow))
                {
                    return true;
                }
            }
        }

        var recoveryCodeHash = recoveryCodeService.HashCode(user.Id, verificationCode);
        return await recoveryCodeRepository.ConsumeAsync(user.Id, recoveryCodeHash, utcNow, cancellationToken);
    }

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private IActionResult ValidationProblem(Dictionary<string, string[]> errors, string errorCode)
    {
        var details = new ProblemDetails
        {
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest
        };

        details.Extensions["errorCode"] = errorCode;
        details.Extensions["errors"] = errors;
        return StatusCode(StatusCodes.Status400BadRequest, details);
    }

    private ObjectResult UnauthorizedProblem() => Problem(
        title: "Unauthorized",
        detail: "Authentication is required to access security settings.",
        statusCode: StatusCodes.Status401Unauthorized);

    private ObjectResult NotFoundProblem(string title, string detail) => Problem(
        title: title,
        detail: detail,
        statusCode: StatusCodes.Status404NotFound);
}

public sealed record MfaSetupConfirmationRequest(
    string SharedKey,
    string VerificationCode);
