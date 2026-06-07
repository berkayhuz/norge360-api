// <copyright file="LoginCommandHandler.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Identity;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Application.Exceptions;
using Norge360.Auth.Application.Features.Commands;
using Norge360.Auth.Application.Helpers;
using Norge360.Auth.Contracts.IntegrationEvents;
using Norge360.Auth.Contracts.Responses;
using Norge360.Auth.Domain.Entities;
using Norge360.Clock;
using Norge360.Notification.Contracts.IntegrationEvents.V1;
using Norge360.Notification.Contracts.Notifications.Enums;
using Norge360.Notification.Contracts.Notifications.Models;

namespace Norge360.Auth.Application.Features.Handlers;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IUsernameLoginResolver usernameLoginResolver,
    IAuthUserProfileResolver authUserProfileResolver,
    IUserSessionRepository userSessionRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IUserMfaRecoveryCodeRepository recoveryCodeRepository,
    IIntegrationEventOutbox integrationEventOutbox,
    IAuthUnitOfWork unitOfWork,
    IAuthenticatorKeyProtector authenticatorKeyProtector,
    IAuthenticatorTotpService authenticatorTotpService,
    IRecoveryCodeService recoveryCodeService,
    IPasswordHasher<User> passwordHasher,
    IAccessTokenFactory accessTokenFactory,
    IRefreshTokenService refreshTokenService,
    IClock clock)
    : IRequestHandler<LoginCommand, AuthenticationTokenResponse>
{
    public async Task<AuthenticationTokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedIdentity = AuthenticationNormalization.Normalize(request.EmailOrUserName);
        var user = await ResolveUserAsync(request.EmailOrUserName, normalizedIdentity, cancellationToken)
            ?? throw new AuthApplicationException(
                title: "Invalid credentials",
                detail: "Email/username or password is incorrect.",
                statusCode: 401,
                errorCode: "invalid_credentials");

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await PublishSecurityNotificationAsync(
                user,
                request.EmailOrUserName,
                "failed_login",
                "Failed sign-in attempt",
                "We detected a failed sign-in attempt on your Norge360 account.",
                "A failed sign-in attempt was detected on your Norge360 account.",
                request.IpAddress,
                request.UserAgent,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthApplicationException(
                title: "Invalid credentials",
                detail: "Email/username or password is incorrect.",
                statusCode: 401,
                errorCode: "invalid_credentials");
        }

        var utcNow = clock.UtcDateTime;
        if (user.MfaEnabled)
        {
            var recoveryCode = request.RecoveryCode?.Trim() ?? string.Empty;
            var mfaCode = request.MfaCode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(recoveryCode) && string.IsNullOrWhiteSpace(mfaCode))
            {
                throw new AuthApplicationException(
                    title: "Multi-factor authentication required",
                    detail: "Enter your authenticator code or a recovery code to continue.",
                    statusCode: 401,
                    errorCode: "mfa_required");
            }

            if (!string.IsNullOrWhiteSpace(recoveryCode))
            {
                var recoveryCodeHash = recoveryCodeService.HashCode(user.Id, recoveryCode);
                var consumed = await recoveryCodeRepository.ConsumeAsync(user.Id, recoveryCodeHash, utcNow, cancellationToken);
                if (!consumed)
                {
                    throw new AuthApplicationException(
                        title: "Invalid recovery code",
                        detail: "The recovery code is invalid.",
                        statusCode: 401,
                        errorCode: "invalid_recovery_code");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.AuthenticatorKeyProtected))
                {
                    throw new AuthApplicationException(
                        title: "Multi-factor authentication unavailable",
                        detail: "An authenticator is not configured for this account.",
                        statusCode: 401,
                        errorCode: "mfa_required");
                }

                var sharedKey = authenticatorKeyProtector.Unprotect(user.AuthenticatorKeyProtected);
                var verified = authenticatorTotpService.VerifyCode(sharedKey, mfaCode, utcNow);
                if (!verified)
                {
                    throw new AuthApplicationException(
                        title: "Invalid MFA code",
                        detail: "The authenticator code is invalid.",
                        statusCode: 401,
                        errorCode: "invalid_mfa_code");
                }
            }
        }

        user.LastLoginAt = utcNow;
        var refreshToken = refreshTokenService.Generate(request.RememberMe);
        var session = new UserSession
        {
            UserId = user.Id,
            IsPersistent = request.RememberMe,
            RefreshTokenFamilyId = Guid.NewGuid(),
            RefreshTokenHash = refreshToken.Hash,
            RefreshTokenExpiresAt = refreshToken.ExpiresAtUtc,
            CreatedAt = utcNow,
            LastSeenAt = utcNow,
            LastRefreshedAt = utcNow,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        await userSessionRepository.AddAsync(session, cancellationToken);

        var deviceFingerprint = BuildDeviceFingerprint(request.UserAgent);
        var trustedDevice = await trustedDeviceRepository.FindActiveByFingerprintAsync(user.Id, deviceFingerprint, cancellationToken);
        var isNewTrustedDevice = trustedDevice is null;
        if (trustedDevice is null)
        {
            trustedDevice = new TrustedDevice
            {
                UserId = user.Id,
                DeviceFingerprintHash = deviceFingerprint,
                DeviceName = DescribeDeviceName(request.UserAgent),
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                TrustedAtUtc = utcNow,
                LastSeenAtUtc = utcNow
            };
            await trustedDeviceRepository.AddAsync(trustedDevice, cancellationToken);
        }
        else
        {
            trustedDevice.MarkSeen(utcNow, request.IpAddress, request.UserAgent);
        }

        if (isNewTrustedDevice)
        {
            await PublishSecurityNotificationAsync(
                user,
                request.EmailOrUserName,
                "suspicious_login",
                "New device sign-in",
                "A new device signed in to your Norge360 account.",
                "New device sign-in detected on your Norge360 account.",
                request.IpAddress,
                request.UserAgent,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userName = (await authUserProfileResolver.ResolveAsync(user.Id, cancellationToken))?.UserName
            ?? $"user-{user.Id:N}";
        var accessToken = accessTokenFactory.Create(
            user.Id,
            userName,
            user.Email ?? string.Empty,
            user.TokenVersion,
            user.GetRoles(),
            user.GetPermissions(),
            session.Id);
        return new AuthenticationTokenResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            user.Id,
            userName,
            user.Email ?? string.Empty,
            session.Id,
            request.RememberMe);
    }

    private async Task<User?> ResolveUserAsync(
        string rawIdentity,
        string normalizedIdentity,
        CancellationToken cancellationToken)
    {
        if (LooksLikeEmail(rawIdentity))
        {
            return await userRepository.FindByNormalizedEmailAsync(normalizedIdentity, cancellationToken);
        }

        var resolvedAuthUserId = await usernameLoginResolver.ResolveAuthUserIdAsync(normalizedIdentity, cancellationToken);
        if (!resolvedAuthUserId.HasValue)
        {
            return null;
        }

        return await userRepository.GetActiveByIdAsync(resolvedAuthUserId.Value, cancellationToken);
    }

    private static bool LooksLikeEmail(string identity) =>
        identity.Contains('@', StringComparison.Ordinal);

    private async Task PublishSecurityNotificationAsync(
        User user,
        string recipientDisplayName,
        string securityEventType,
        string subject,
        string textBody,
        string htmlBody,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        await integrationEventOutbox.AddAsync(
            eventId: Guid.NewGuid(),
            eventName: SecurityNotificationRequestedV1.EventName,
            eventVersion: SecurityNotificationRequestedV1.EventVersion,
            routingKey: SecurityNotificationRequestedV1.RoutingKey,
            source: "Norge360.Auth",
            payload: new SecurityNotificationRequestedV1(
                Guid.NewGuid(),
                user.Id,
                new NotificationRecipient(user.Id, user.Email, null, null, recipientDisplayName.Trim()),
                securityEventType,
                [NotificationChannel.Email],
                subject,
                textBody,
                htmlBody,
                new NotificationTemplateData(
                    TemplateKey: $"auth.security.{securityEventType}",
                    Values: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["displayName"] = recipientDisplayName.Trim(),
                        ["ipAddress"] = ipAddress ?? string.Empty,
                        ["userAgent"] = userAgent ?? string.Empty,
                        ["securityEventType"] = securityEventType
                    }),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["displayName"] = recipientDisplayName.Trim(),
                    ["ipAddress"] = ipAddress ?? string.Empty,
                    ["userAgent"] = userAgent ?? string.Empty,
                    ["securityEventType"] = securityEventType
                },
                CorrelationId: null,
                IdempotencyKey: $"{securityEventType}:{user.Id:N}:{DateTime.UtcNow:O}",
                OccurredAtUtc: clock.UtcDateTime),
            correlationId: null,
            traceId: null,
            occurredAtUtc: clock.UtcDateTime,
            cancellationToken: cancellationToken);
    }

    private static string BuildDeviceFingerprint(string? userAgent)
    {
        var normalized = string.IsNullOrWhiteSpace(userAgent) ? "unknown-user-agent" : userAgent.Trim();
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized)));
    }

    private static string DescribeDeviceName(string? userAgent)
    {
        var ua = userAgent ?? string.Empty;
        if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows device";
        }

        if (ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) || ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase))
        {
            return "Mac device";
        }

        if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Android device";
        }

        if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "Apple device";
        }

        return "Signed-in device";
    }
}
