// <copyright file="RefreshTokenCommandHandler.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Application.Features.Commands;
using Norge360.Auth.Contracts.Responses;
using Norge360.Clock;

namespace Norge360.Auth.Application.Features.Handlers;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IUserSessionRepository userSessionRepository,
    IAuthUserProfileResolver authUserProfileResolver,
    IAuthUnitOfWork unitOfWork,
    IAccessTokenFactory accessTokenFactory,
    IRefreshTokenService refreshTokenService,
    IClock clock)
    : IRequestHandler<RefreshTokenCommand, AuthenticationTokenResponse>
{
    public async Task<AuthenticationTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var session = await userSessionRepository.GetWithUserAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException("invalid_refresh_token");

        if (session.IsRevoked || !refreshTokenService.Verify(request.RefreshToken, session.RefreshTokenHash))
        {
            throw new InvalidOperationException("invalid_refresh_token");
        }

        var user = session.User ?? await userRepository.GetByIdAsync(session.UserId, cancellationToken)
            ?? throw new InvalidOperationException("invalid_refresh_token");

        var utcNow = clock.UtcDateTime;
        var refreshToken = refreshTokenService.Generate(session.IsPersistent);
        session.RefreshTokenHash = refreshToken.Hash;
        session.RefreshTokenExpiresAt = refreshToken.ExpiresAtUtc;
        session.MarkRefreshRotated(utcNow);

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
            session.IsPersistent);
    }
}
