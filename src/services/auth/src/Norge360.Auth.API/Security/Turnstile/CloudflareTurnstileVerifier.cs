// <copyright file="CloudflareTurnstileVerifier.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Norge360.Auth.API.Security.Turnstile;

public sealed class CloudflareTurnstileVerifier(
    IHttpClientFactory httpClientFactory,
    IOptions<TurnstileOptions> options,
    IHostEnvironment environment,
    ILogger<CloudflareTurnstileVerifier> logger) : ITurnstileVerifier
{
    private static readonly Uri SiteVerifyEndpoint = new("https://challenges.cloudflare.com/turnstile/v0/siteverify");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TurnstileVerificationResult> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            if (environment.IsProduction())
            {
                return TurnstileVerificationResult.Fail("turnstile_disabled", "Turnstile must be enabled in production.");
            }

            logger.LogWarning("Turnstile verification is bypassed because Cloudflare:Turnstile:Enabled is false.");
            return TurnstileVerificationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return TurnstileVerificationResult.Fail("turnstile_token_missing", "Turnstile token is required.", StatusCodes.Status400BadRequest);
        }

        var secretKey = options.Value.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            if (environment.IsDevelopment())
            {
                return TurnstileVerificationResult.Fail("turnstile_secret_missing", "Turnstile secret is not configured for development.", StatusCodes.Status400BadRequest);
            }

            logger.LogError("Turnstile secret key is missing.");
            return TurnstileVerificationResult.Fail("turnstile_unavailable", "Turnstile verification is unavailable.");
        }

        try
        {
            var form = new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                form["remoteip"] = remoteIp;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, SiteVerifyEndpoint)
            {
                Content = new FormUrlEncodedContent(form)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var client = httpClientFactory.CreateClient(nameof(CloudflareTurnstileVerifier));
            using var response = await client.SendAsync(request, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<TurnstileSiteVerifyResponse>(stream, JsonOptions, cancellationToken);

            if (payload is null)
            {
                logger.LogWarning("Turnstile verification returned an empty payload.");
                return TurnstileVerificationResult.Fail("turnstile_invalid_response", "Turnstile validation response is invalid.");
            }

            if (!payload.Success)
            {
                var cloudflareCode = payload.ErrorCodes.FirstOrDefault() ?? "unknown";
                logger.LogInformation("Turnstile verification failed with code {CloudflareErrorCode}.", cloudflareCode);
                return TurnstileVerificationResult.Fail("turnstile_validation_failed", "Turnstile validation failed.");
            }

            if (!IsHostnameAllowed(payload.Hostname))
            {
                logger.LogWarning("Turnstile hostname validation failed for hostname {Hostname}.", payload.Hostname);
                return TurnstileVerificationResult.Fail("turnstile_hostname_invalid", "Turnstile hostname validation failed.");
            }

            return TurnstileVerificationResult.Success();
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Turnstile verification request timed out.");
            return TurnstileVerificationResult.Fail("turnstile_timeout", "Turnstile verification timed out.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Turnstile verification request failed.");
            return TurnstileVerificationResult.Fail("turnstile_network_error", "Turnstile verification could not be completed.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Turnstile verification response could not be parsed.");
            return TurnstileVerificationResult.Fail("turnstile_invalid_response", "Turnstile validation response is invalid.");
        }
    }

    private bool IsHostnameAllowed(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return false;
        }

        if (environment.IsDevelopment())
        {
            if (hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                hostname.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var allowed = options.Value.AllowedHostnames;
        return allowed.Any(item => item.Equals(hostname, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record TurnstileSiteVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("challenge_ts")] string? ChallengeTimestamp,
        [property: JsonPropertyName("hostname")] string? Hostname,
        [property: JsonPropertyName("error-codes")] string[] ErrorCodes,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("cdata")] string? CData);
}
