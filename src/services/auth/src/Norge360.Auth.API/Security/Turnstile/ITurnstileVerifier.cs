namespace Norge360.Auth.API.Security.Turnstile;

public interface ITurnstileVerifier
{
    Task<TurnstileVerificationResult> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken);
}

