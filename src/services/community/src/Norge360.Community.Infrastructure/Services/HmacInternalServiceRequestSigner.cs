// <copyright file="HmacInternalServiceRequestSigner.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Norge360.Community.Infrastructure.Options;

namespace Norge360.Community.Infrastructure.Services;

internal sealed class HmacInternalServiceRequestSigner(IOptions<InternalServiceSigningOptions> options) : IInternalServiceRequestSigner
{
    public async ValueTask SignAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var value = options.Value;
        if (!value.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value.Secret))
        {
            throw new InvalidOperationException("internal_signing_secret_missing");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var method = request.Method.Method.ToUpperInvariant();
        var pathAndQuery = GetPathAndQuery(request.RequestUri);
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var canonical = string.Join("\n", [value.ServiceName, method, pathAndQuery, timestamp, nonce, bodyHash]);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(value.Secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));

        request.Headers.Remove("X-Norge360-Service");
        request.Headers.Remove("X-Norge360-Timestamp");
        request.Headers.Remove("X-Norge360-Nonce");
        request.Headers.Remove("X-Norge360-Signature");

        request.Headers.TryAddWithoutValidation("X-Norge360-Service", value.ServiceName);
        request.Headers.TryAddWithoutValidation("X-Norge360-Timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("X-Norge360-Nonce", nonce);
        request.Headers.TryAddWithoutValidation("X-Norge360-Signature", signature);
    }

    private static string GetPathAndQuery(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return "/";
        }

        if (requestUri.IsAbsoluteUri)
        {
            return requestUri.PathAndQuery;
        }

        var value = requestUri.OriginalString;
        return value.StartsWith("/", StringComparison.Ordinal) ? value : $"/{value}";
    }
}
