using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Norge360.Accounts.API.Security;
using Norge360.Community.Infrastructure.Services;
using Xunit;
using AccountsSigningOptions = Norge360.Accounts.API.Options.InternalServiceSigningOptions;
using AccountsSigningOptionsValidation = Norge360.Accounts.API.Security.InternalServiceSigningOptionsValidation;
using CommunitySigningOptions = Norge360.Community.Infrastructure.Options.InternalServiceSigningOptions;
using CommunitySigningOptionsValidation = Norge360.Community.Infrastructure.Options.InternalServiceSigningOptionsValidation;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityInternalSigningTests
{
    private const string Secret = "integration-test-signing-secret";

    [Fact]
    public async Task SignerAndValidator_ShouldAcceptValidSignatureOnceAndRejectReplay()
    {
        var signer = CreateSigner();
        using var message = CreateMessage("""{"userIds":[]}""");
        await signer.SignAsync(message, CancellationToken.None);
        var validator = CreateValidator();

        var first = await validator.ValidateAsync(ToHttpRequest(message), CancellationToken.None);
        var replay = await validator.ValidateAsync(ToHttpRequest(message), CancellationToken.None);

        first.Should().BeTrue();
        replay.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ShouldRejectInvalidSecretChangedBodyOldTimestampAndMissingNonce()
    {
        var validator = CreateValidator();

        using var wrongSecret = CreateMessage("""{"userIds":[]}""");
        AddSignedHeaders(wrongSecret, "wrong-secret", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "nonce-1");
        (await validator.ValidateAsync(ToHttpRequest(wrongSecret), CancellationToken.None)).Should().BeFalse();

        using var changedBody = CreateMessage("""{"userIds":[]}""");
        AddSignedHeaders(changedBody, Secret, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "nonce-2");
        (await validator.ValidateAsync(ToHttpRequest(changedBody, """{"userIds":["changed"]}"""), CancellationToken.None)).Should().BeFalse();

        using var oldTimestamp = CreateMessage("""{"userIds":[]}""");
        AddSignedHeaders(oldTimestamp, Secret, DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(), "nonce-3");
        (await validator.ValidateAsync(ToHttpRequest(oldTimestamp), CancellationToken.None)).Should().BeFalse();

        using var missingNonce = CreateMessage("""{"userIds":[]}""");
        AddSignedHeaders(missingNonce, Secret, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "nonce-4");
        missingNonce.Headers.Remove("X-Norge360-Nonce");
        (await validator.ValidateAsync(ToHttpRequest(missingNonce), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task DisabledMode_ShouldSkipHeadersAndAcceptRequest()
    {
        var signer = new NoOpInternalServiceRequestSigner();
        using var message = CreateMessage("""{"userIds":[]}""");
        await signer.SignAsync(message, CancellationToken.None);
        var validator = new HmacInternalServiceRequestValidator(
            Options.Create(new AccountsSigningOptions { Enabled = false }),
            NullLogger<HmacInternalServiceRequestValidator>.Instance);

        message.Headers.Should().BeEmpty();
        (await validator.ValidateAsync(ToHttpRequest(message), CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public void OptionsValidation_ShouldRejectEnabledModeWithoutSecretOrServiceName()
    {
        var accountsResult = new AccountsSigningOptionsValidation().Validate(null, new AccountsSigningOptions { Enabled = true, Secret = "", ServiceName = "" });
        var communityResult = new CommunitySigningOptionsValidation().Validate(null, new CommunitySigningOptions { Enabled = true, Secret = "", ServiceName = "" });

        accountsResult.Failed.Should().BeTrue();
        communityResult.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorProvider_ShouldAddSignerHeadersToBatchSummaryRequest()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"items":[]}""", Encoding.UTF8, "application/json")
            };
        });
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(x => x.CreateClient("accounts-community")).Returns(new HttpClient(handler) { BaseAddress = new Uri("http://accounts.test") });
        var provider = new AccountsCommunityAuthorProfileProvider(
            clientFactory.Object,
            new HttpContextAccessor(),
            CreateSigner(),
            NullLogger<AccountsCommunityAuthorProfileProvider>.Instance);

        await provider.GetAuthorSummariesAsync([Guid.NewGuid()], null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Headers.Contains("X-Norge360-Service").Should().BeTrue();
        captured.Headers.Contains("X-Norge360-Timestamp").Should().BeTrue();
        captured.Headers.Contains("X-Norge360-Nonce").Should().BeTrue();
        captured.Headers.Contains("X-Norge360-Signature").Should().BeTrue();
    }

    [Fact]
    public async Task AuthorProvider_ShouldReturnMinimalFallbackWhenBatchEndpointFails()
    {
        var calls = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(x => x.CreateClient("accounts-community")).Returns(new HttpClient(handler) { BaseAddress = new Uri("http://accounts.test") });
        var provider = new AccountsCommunityAuthorProfileProvider(
            clientFactory.Object,
            new HttpContextAccessor(),
            new NoOpInternalServiceRequestSigner(),
            NullLogger<AccountsCommunityAuthorProfileProvider>.Instance);
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var result = await provider.GetAuthorSummariesAsync([currentUserId, otherUserId], currentUserId, CancellationToken.None);

        calls.Should().Be(1);
        result[currentUserId].CanViewPosts.Should().BeTrue();
        result[otherUserId].CanViewPosts.Should().BeFalse();
        result[otherUserId].Username.Should().BeNull();
    }

    private static HmacInternalServiceRequestSigner CreateSigner() =>
        new(Options.Create(new CommunitySigningOptions { Enabled = true, Secret = Secret, ServiceName = "community-api" }));

    private static HmacInternalServiceRequestValidator CreateValidator() =>
        new(
            Options.Create(new AccountsSigningOptions { Enabled = true, Secret = Secret, ServiceName = "community-api", ClockSkewSeconds = 120 }),
            NullLogger<HmacInternalServiceRequestValidator>.Instance);

    private static HttpRequestMessage CreateMessage(string body) =>
        new(HttpMethod.Post, "/api/accounts/internal/users/batch-summary") { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpRequest ToHttpRequest(HttpRequestMessage message, string? bodyOverride = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = message.Method.Method;
        context.Request.Path = "/api/accounts/internal/users/batch-summary";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyOverride ?? message.Content!.ReadAsStringAsync().GetAwaiter().GetResult()));
        foreach (var header in message.Headers)
        {
            context.Request.Headers[header.Key] = header.Value.ToArray();
        }
        return context.Request;
    }

    private static void AddSignedHeaders(HttpRequestMessage message, string secret, long timestamp, string nonce)
    {
        var body = message.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var canonical = string.Join("\n", ["community-api", "POST", "/api/accounts/internal/users/batch-summary", timestamp.ToString(), nonce, bodyHash]);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
        message.Headers.TryAddWithoutValidation("X-Norge360-Service", "community-api");
        message.Headers.TryAddWithoutValidation("X-Norge360-Timestamp", timestamp.ToString());
        message.Headers.TryAddWithoutValidation("X-Norge360-Nonce", nonce);
        message.Headers.TryAddWithoutValidation("X-Norge360-Signature", signature);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
