using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Norge360.Accounts.API.Options;

namespace Norge360.Accounts.API.Security;

public interface IInternalServiceRequestValidator
{
    Task<bool> ValidateAsync(HttpRequest request, CancellationToken cancellationToken);
}

public sealed class HmacInternalServiceRequestValidator(
    IOptions<InternalServiceSigningOptions> options,
    ILogger<HmacInternalServiceRequestValidator> logger) : IInternalServiceRequestValidator
{
    private readonly ConcurrentDictionary<string, long> usedNonces = new(StringComparer.Ordinal);

    public async Task<bool> ValidateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var value = options.Value;
        if (!value.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value.Secret) || string.IsNullOrWhiteSpace(value.ServiceName))
        {
            logger.LogWarning("Internal signing validation failed: config missing.");
            return false;
        }

        if (!request.Headers.TryGetValue("X-Norge360-Service", out var service) || !string.Equals(service.ToString(), value.ServiceName, StringComparison.Ordinal))
        {
            logger.LogWarning("Internal signing validation failed: invalid service header.");
            return false;
        }

        if (!request.Headers.TryGetValue("X-Norge360-Timestamp", out var timestampRaw))
        {
            logger.LogWarning("Internal signing validation failed: missing timestamp.");
            return false;
        }

        if (!request.Headers.TryGetValue("X-Norge360-Nonce", out var nonce) || string.IsNullOrWhiteSpace(nonce.ToString()))
        {
            logger.LogWarning("Internal signing validation failed: missing nonce.");
            return false;
        }

        if (!request.Headers.TryGetValue("X-Norge360-Signature", out var signature) || string.IsNullOrWhiteSpace(signature.ToString()))
        {
            logger.LogWarning("Internal signing validation failed: missing signature.");
            return false;
        }

        if (!long.TryParse(timestampRaw.ToString(), out var timestamp))
        {
            logger.LogWarning("Internal signing validation failed: invalid timestamp format.");
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > value.ClockSkewSeconds)
        {
            logger.LogWarning("Internal signing validation failed: timestamp skew exceeded.");
            return false;
        }

        request.EnableBuffering();
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }
        string body;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        request.Body.Position = 0;

        var method = request.Method.ToUpperInvariant();
        var pathAndQuery = request.Path + request.QueryString;
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var canonical = string.Join("\n", [value.ServiceName, method, pathAndQuery, timestampRaw.ToString(), nonce.ToString(), bodyHash]);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(value.Secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature.ToString())))
        {
            logger.LogWarning(
                "Internal signing validation failed: signature mismatch. method={Method} path={Path} bodyHashPrefix={BodyHashPrefix} sigLength={SigLength}",
                method,
                pathAndQuery.ToString(),
                bodyHash[..Math.Min(bodyHash.Length, 8)],
                signature.ToString().Length);
            return false;
        }

        foreach (var usedNonce in usedNonces.Where(x => x.Value < now - value.ClockSkewSeconds))
        {
            usedNonces.TryRemove(usedNonce.Key, out _);
        }

        var accepted = usedNonces.TryAdd($"{value.ServiceName}:{nonce}", timestamp);
        if (!accepted)
        {
            logger.LogWarning("Internal signing validation failed: replay nonce detected.");
        }

        return accepted;
    }
}
