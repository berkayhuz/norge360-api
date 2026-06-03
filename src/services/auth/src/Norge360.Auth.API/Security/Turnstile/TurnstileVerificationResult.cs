// <copyright file="TurnstileVerificationResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Http;

namespace Norge360.Auth.API.Security.Turnstile;

public sealed record TurnstileVerificationResult(
    bool IsSuccess,
    int StatusCode,
    string ErrorCode,
    string Message)
{
    public static TurnstileVerificationResult Success() => new(true, StatusCodes.Status200OK, string.Empty, string.Empty);

    public static TurnstileVerificationResult Fail(string errorCode, string message, int statusCode = StatusCodes.Status403Forbidden)
        => new(false, statusCode, errorCode, message);
}
