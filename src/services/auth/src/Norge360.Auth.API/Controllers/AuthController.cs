// <copyright file="AuthController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net.Mail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Norge360.Auth.API.Accessors;
using Norge360.Auth.API.Cookies;
using Norge360.Auth.API.Security;
using Norge360.Auth.API.Security.Turnstile;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Application.Exceptions;
using Norge360.Auth.Application.Features.Commands;
using Norge360.Auth.Application.Options;
using Norge360.Auth.Application.Records;
using Norge360.Auth.Contracts.IntegrationEvents;
using Norge360.Auth.Contracts.Internal;
using Norge360.Auth.Contracts.Requests;
using Norge360.Auth.Contracts.Responses;
using Norge360.Auth.Domain.Entities;
using Norge360.Clock;
using Norge360.Localization;

namespace Norge360.Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ISender sender,
    AuthRequestContextAccessor requestContextAccessor,
    AuthCookieService cookieService,
    IUserRepository userRepository,
    IUserSessionRepository userSessionRepository,
    IAuthVerificationTokenRepository verificationTokenRepository,
    IAuthVerificationTokenService verificationTokenService,
    IIntegrationEventOutbox integrationEventOutbox,
    IAuthUnitOfWork unitOfWork,
    IAuthUserProfileResolver authUserProfileResolver,
    IPasswordHasher<User> passwordHasher,
    IClock clock,
    IOptions<AccountLifecycleOptions> accountLifecycleOptions,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions) : ControllerBase
{
    private static readonly AccountActionAcceptedResponse AccountActionAccepted = new(
        "If the request can be completed, instructions will be sent.");

    [HttpPost("register")]
    [AllowAnonymous]
    [RequireTurnstile]
    [EnableRateLimiting(AuthRateLimitingOptions.RegisterPolicyName)]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthIssuedSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<AccountActionAcceptedResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new RegisterCommand(
                request.UserName,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                Norge360Cultures.NormalizeOrDefault(request.Culture ?? HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()?.RequestCulture.Culture.Name),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        if (response is AuthSessionResult.PendingConfirmation)
        {
            return Accepted(AccountActionAccepted);
        }

        var issued = (AuthSessionResult.Issued)response;
        cookieService.Apply(Response, issued.Tokens);
        return Ok(cookieService.CreateResponsePayload(issued.Tokens));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [RequireTurnstile]
    [EnableRateLimiting(AuthRateLimitingOptions.LoginPolicyName)]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthIssuedSessionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new LoginCommand(
                request.EmailOrUserName,
                request.Password,
                request.RememberMe,
                request.MfaCode,
                request.RecoveryCode,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        cookieService.Apply(Response, response);
        return Ok(cookieService.CreateResponsePayload(response));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitingOptions.RefreshPolicyName)]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthIssuedSessionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshContext = requestContextAccessor.ResolveRefreshContext(Request, request.SessionId, request.RefreshToken);

        var response = await sender.Send(
            new RefreshTokenCommand(
                refreshContext.SessionId,
                refreshContext.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        cookieService.Apply(Response, response);
        return Ok(cookieService.CreateResponsePayload(response));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitingOptions.LogoutPolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var refreshContext = requestContextAccessor.ResolveRefreshContext(
            Request,
            request?.SessionId ?? Guid.Empty,
            request?.RefreshToken);
        PrincipalContext? principalContext = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            principalContext = requestContextAccessor.GetPrincipalContext(User);
        }

        if (refreshContext.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(refreshContext.RefreshToken))
        {
            cookieService.Clear(Response);
            return NoContent();
        }

        var sessionId = refreshContext.SessionId == Guid.Empty && principalContext is not null
            ? principalContext.CurrentSessionId
            : refreshContext.SessionId;

        if (sessionId != Guid.Empty && !string.IsNullOrWhiteSpace(refreshContext.RefreshToken))
        {
            try
            {
                await sender.Send(new LogoutCommand(sessionId, refreshContext.RefreshToken), cancellationToken);
            }
            catch (AuthApplicationException ex) when (
                ex.ErrorCode is "session_not_found" or "invalid_refresh_token")
            {
            }
        }

        cookieService.Clear(Response);
        return NoContent();
    }

    [HttpGet("session-status")]
    [Authorize]
    [ProducesResponseType<AuthSessionStatusResponse>(StatusCodes.Status200OK)]
    public IActionResult GetSessionStatus()
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var roles = User.Claims
            .Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role || claim.Type == "role")
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var permissions = User.Claims
            .Where(claim => claim.Type == "permission" || claim.Type == "permissions")
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new AuthSessionStatusResponse(
            principal.UserId,
            principal.CurrentSessionId,
            principal.Email ?? string.Empty,
            roles,
            permissions,
            AccountStatus: "active",
            EmailConfirmed: true,
            MfaVerifiedAt: null));
    }

    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType<SessionSummaryResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var sessions = await userSessionRepository.ListForUserAsync(principal.UserId, cancellationToken);
        var response = sessions.Select(session => new SessionSummaryResponse(
            session.Id,
            session.Id == principal.CurrentSessionId,
            session.IsRevoked,
            session.IpAddress,
            session.UserAgent,
            session.CreatedAt,
            session.LastSeenAt,
            session.RefreshTokenExpiresAt,
            session.RevokedAt,
            session.RevokedReason)).ToArray();

        return Ok(response);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var revoked = await userSessionRepository.RevokeAsync(
            principal.UserId,
            sessionId,
            clock.UtcDateTime,
            "revoked_by_user",
            cancellationToken);

        if (!revoked)
        {
            return NotFoundProblem("Session not found.", "The session could not be found.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("sessions/revoke-others")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeOtherSessions(CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        await userSessionRepository.RevokeAllAsync(
            principal.UserId,
            clock.UtcDateTime,
            "revoked_by_user",
            excludedSessionId: principal.CurrentSessionId,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting(AuthRateLimitingOptions.PasswordRecoveryPolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var utcNow = clock.UtcDateTime;
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var currentPassword = request.CurrentPassword?.Trim() ?? string.Empty;
        var currentPasswordVerification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (currentPasswordVerification == PasswordVerificationResult.Failed)
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = ["Current password is incorrect."]
                },
                "invalid_current_password");
        }

        var newPassword = request.NewPassword?.Trim() ?? string.Empty;
        var passwordErrors = ValidatePassword(newPassword);
        if (passwordErrors.Count > 0)
        {
            return ValidationProblem(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["newPassword"] = passwordErrors.ToArray()
            }, "password_validation_failed");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.PasswordChangedAt = utcNow;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.TokenVersion++;
        user.ForcePasswordChange = false;

        await userSessionRepository.RevokeAllAsync(user.Id, utcNow, "password_changed", excludedSessionId: null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("change-email")]
    [Authorize]
    [EnableRateLimiting(AuthRateLimitingOptions.EmailConfirmationPolicyName)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var principal = requestContextAccessor.GetPrincipalContext(User);
        var utcNow = clock.UtcDateTime;
        var user = await userRepository.GetActiveByIdAsync(principal.UserId, cancellationToken);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var currentPassword = request.CurrentPassword?.Trim() ?? string.Empty;
        var currentPasswordVerification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (currentPasswordVerification == PasswordVerificationResult.Failed)
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = ["Current password is incorrect."]
                },
                "invalid_current_password");
        }

        var newEmail = request.NewEmail?.Trim() ?? string.Empty;
        if (!IsValidEmail(newEmail))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["newEmail"] = ["Enter a valid email address."]
                },
                "invalid_email");
        }

        var normalizedNewEmail = newEmail.Trim().ToUpperInvariant();
        if (string.Equals(normalizedNewEmail, user.NormalizedEmail, StringComparison.Ordinal))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["newEmail"] = ["The new email address is the same as the current one."]
                },
                "email_unchanged");
        }

        if (await userRepository.ExistsByEmailAsync(normalizedNewEmail, cancellationToken))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["newEmail"] = ["This email address is already in use."]
                },
                "duplicate_email");
        }

        var userName = (await authUserProfileResolver.ResolveAsync(user.Id, cancellationToken))?.UserName
            ?? $"user-{user.Id:N}";
        var token = verificationTokenService.GenerateToken();
        var tokenHash = verificationTokenService.HashToken(token);
        var expirationMinutes = Math.Clamp(accountLifecycleOptions.Value.EmailChangeTokenMinutes, 5, 1440);
        var expiresAtUtc = utcNow.AddMinutes(expirationMinutes);

        await verificationTokenRepository.RevokeOutstandingAsync(
            user.Id,
            AuthVerificationTokenPurpose.EmailChange,
            utcNow,
            normalizedNewEmail,
            cancellationToken);

        await verificationTokenRepository.AddAsync(
            new AuthVerificationToken
            {
                UserId = user.Id,
                Purpose = AuthVerificationTokenPurpose.EmailChange,
                TokenHash = tokenHash,
                Target = normalizedNewEmail,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAt = utcNow
            },
            cancellationToken);

        var confirmationUrl = BuildConfirmationUrl(user.Id, newEmail, token);
        await integrationEventOutbox.AddAsync(
            eventId: Guid.NewGuid(),
            eventName: AuthEmailChangeRequestedV1.EventName,
            eventVersion: AuthEmailChangeRequestedV1.EventVersion,
            routingKey: AuthEmailChangeRequestedV1.RoutingKey,
            source: "Norge360.Auth",
            payload: new AuthEmailChangeRequestedV1(
                user.Id,
                userName,
                user.Email ?? string.Empty,
                newEmail,
                token,
                confirmationUrl,
                expiresAtUtc),
            correlationId: Request.Headers["X-Correlation-Id"].FirstOrDefault(),
            traceId: HttpContext.TraceIdentifier,
            occurredAtUtc: utcNow,
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Accepted(AccountActionAccepted);
    }

    [HttpPost("confirm-email-change")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcDateTime;
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return NotFoundProblem("Email change could not be completed.", "The account could not be found.");
        }

        var tokenValue = request.Token?.Trim() ?? string.Empty;
        var tokenHash = verificationTokenService.HashToken(tokenValue);
        var token = await verificationTokenRepository.GetValidAsync(
            user.Id,
            AuthVerificationTokenPurpose.EmailChange,
            tokenHash,
            utcNow,
            cancellationToken);

        if (token is null)
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["token"] = ["The email change link is invalid or has expired."]
                },
                "invalid_email_change_token");
        }

        var requestedEmail = request.NewEmail?.Trim() ?? string.Empty;
        var normalizedRequestedEmail = requestedEmail.Trim().ToUpperInvariant();
        if (!string.Equals(token.Target, normalizedRequestedEmail, StringComparison.Ordinal))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["newEmail"] = ["The email change link does not match the requested email address."]
                },
                "invalid_email_change_target");
        }

        if (!IsValidEmail(requestedEmail))
        {
            return ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["newEmail"] = ["Enter a valid email address."]
                },
                "invalid_email");
        }

        user.Email = requestedEmail;
        user.NormalizedEmail = normalizedRequestedEmail;
        user.EmailConfirmed = true;
        user.EmailConfirmedAt = utcNow;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.TokenVersion++;

        token.Consume(utcNow, HttpContext.Connection.RemoteIpAddress?.ToString());
        await userSessionRepository.RevokeAllAsync(user.Id, utcNow, "email_changed", excludedSessionId: null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private List<string> ValidatePassword(string password)
    {
        var policy = passwordPolicyOptions.Value;
        var errors = new List<string>();

        if (password.Length < policy.MinimumLength)
        {
            errors.Add($"Password must be at least {policy.MinimumLength} characters long.");
        }

        if (password.Length > policy.MaxLength)
        {
            errors.Add($"Password can be at most {policy.MaxLength} characters long.");
        }

        if (policy.DisallowWhitespace && password.Any(char.IsWhiteSpace))
        {
            errors.Add("Password cannot contain spaces.");
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must include a lowercase letter.");
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must include an uppercase letter.");
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must include a digit.");
        }

        if (policy.RequireNonAlphanumeric && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must include a special character.");
        }

        if (policy.RequiredUniqueChars > 0 && password.Distinct().Count() < policy.RequiredUniqueChars)
        {
            errors.Add($"Password must include at least {policy.RequiredUniqueChars} unique characters.");
        }

        if (policy.BlacklistedPasswords.Any(value => string.Equals(value, password, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("This password is not allowed.");
        }

        return errors;
    }

    private string BuildConfirmationUrl(Guid userId, string newEmail, string token)
    {
        var baseUri = new Uri(accountLifecycleOptions.Value.PublicAppBaseUrl, UriKind.Absolute);
        var confirmationUri = new Uri(baseUri, accountLifecycleOptions.Value.ConfirmEmailChangePath);
        return QueryHelpers.AddQueryString(
            confirmationUri.ToString(),
            new Dictionary<string, string?>
            {
                ["newEmail"] = newEmail,
                ["token"] = token,
                ["userId"] = userId.ToString("D")
            });
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

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
        detail: "Authentication is required to access this resource.",
        statusCode: StatusCodes.Status401Unauthorized);

    private ObjectResult NotFoundProblem(string title, string detail) => Problem(
        title: title,
        detail: detail,
        statusCode: StatusCodes.Status404NotFound);
}
