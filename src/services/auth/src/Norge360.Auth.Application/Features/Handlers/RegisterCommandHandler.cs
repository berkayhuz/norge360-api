// <copyright file="RegisterCommandHandler.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Identity;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Application.Exceptions;
using Norge360.Auth.Application.Features.Commands;
using Norge360.Auth.Application.Helpers;
using Norge360.Auth.Application.Records;
using Norge360.Auth.Contracts.IntegrationEvents;
using Norge360.Auth.Contracts.Responses;
using Norge360.Auth.Domain.Entities;
using Norge360.Clock;

namespace Norge360.Auth.Application.Features.Handlers;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IAuthUserProfileResolver authUserProfileResolver,
    IUserSessionRepository userSessionRepository,
    IAuthUnitOfWork unitOfWork,
    IIntegrationEventOutbox integrationEventOutbox,
    IPasswordHasher<User> passwordHasher,
    IAccessTokenFactory accessTokenFactory,
    IRefreshTokenService refreshTokenService,
    IClock clock)
    : IRequestHandler<RegisterCommand, AuthSessionResult>
{
    public async Task<AuthSessionResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcDateTime;
        var normalizedEmail = AuthenticationNormalization.Normalize(request.Email);

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new AuthApplicationException(
                title: "Registration conflict",
                detail: "A user with the same email already exists.",
                statusCode: 409,
                errorCode: "registration_conflict");
        }

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = false,
            Roles = "user",
            Permissions = "session:self,profile:self",
            CreatedAt = utcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var refreshToken = refreshTokenService.Generate(true);
        var session = new UserSession
        {
            UserId = user.Id,
            IsPersistent = true,
            RefreshTokenFamilyId = Guid.NewGuid(),
            RefreshTokenHash = refreshToken.Hash,
            RefreshTokenExpiresAt = refreshToken.ExpiresAtUtc,
            CreatedAt = utcNow,
            LastSeenAt = utcNow,
            LastRefreshedAt = utcNow
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userSessionRepository.AddAsync(session, cancellationToken);

        await integrationEventOutbox.AddAsync(
            eventId: Guid.NewGuid(),
            eventName: UserRegisteredV1.EventName,
            eventVersion: UserRegisteredV1.EventVersion,
            routingKey: UserRegisteredV1.RoutingKey,
            source: "Norge360.Auth",
            payload: new UserRegisteredV1(
                UserId: user.Id,
                UserName: request.UserName.Trim(),
                Email: user.Email ?? string.Empty,
                FirstName: AuthenticationNormalization.CleanOrNull(request.FirstName),
                LastName: AuthenticationNormalization.CleanOrNull(request.LastName),
                RegisteredAtUtc: utcNow,
                Culture: request.Culture),
            correlationId: null,
            traceId: null,
            occurredAtUtc: utcNow,
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userName = (await authUserProfileResolver.ResolveAsync(user.Id, cancellationToken))?.UserName
            ?? request.UserName.Trim();
        var accessToken = accessTokenFactory.Create(
            user.Id,
            userName,
            user.Email ?? string.Empty,
            user.TokenVersion,
            user.GetRoles(),
            user.GetPermissions(),
            session.Id);
        return new AuthSessionResult.Issued(
            new AuthenticationTokenResponse(
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                refreshToken.Token,
                refreshToken.ExpiresAtUtc,
                user.Id,
                userName,
                user.Email ?? string.Empty,
                session.Id,
                IsPersistent: true));
    }
}
