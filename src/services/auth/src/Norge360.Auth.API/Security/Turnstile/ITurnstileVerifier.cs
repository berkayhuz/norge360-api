// <copyright file="ITurnstileVerifier.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Auth.API.Security.Turnstile;

public interface ITurnstileVerifier
{
    Task<TurnstileVerificationResult> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken);
}

