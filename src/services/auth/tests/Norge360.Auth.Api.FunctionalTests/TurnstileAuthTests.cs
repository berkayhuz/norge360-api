using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Norge360.Auth.API.Controllers;
using Norge360.Auth.API.Security.Turnstile;
using Norge360.Auth.Application.Features.Commands;
using Norge360.Auth.Contracts.Requests;
using Norge360.Auth.Contracts.Responses;
using Norge360.Auth.TestKit.Fixtures;

namespace Norge360.Auth.Api.FunctionalTests;

public sealed class TurnstileAuthTests
{
    [Fact]
    public void PublicPostAuthEndpoints_ShouldRequireTurnstile_ExceptRefreshAndLogout()
    {
        var actions = typeof(AuthController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method =>
            {
                var httpPost = method.GetCustomAttribute<HttpPostAttribute>();
                var isAllowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                return httpPost is not null && isAllowAnonymous;
            })
            .ToArray();

        var exemptActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(AuthController.Refresh),
            nameof(AuthController.Logout)
        };

        var missingAttribute = actions
            .Where(method => !exemptActions.Contains(method.Name))
            .Where(method => method.GetCustomAttribute<RequireTurnstileAttribute>() is null)
            .Select(method => method.Name)
            .ToArray();

        Assert.True(missingAttribute.Length == 0, $"RequireTurnstile missing on: {string.Join(", ", missingAttribute)}");
    }

    [Fact]
    public async Task AttributeEndpoint_ShouldReject_WhenTokenMissing_AndSkipAction()
    {
        await using var factory = new AuthWebApplicationFactory();
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user@norge360.com", "password"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Turnstile verification failed", problem?.Title);
        factory.SenderMock.Verify(sender => sender.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AttributeEndpoint_Register_ShouldReject_WhenTokenMissing_AndSkipAction()
    {
        await using var factory = new AuthWebApplicationFactory();
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("acme-user", "user@norge360.com", "StrongPassword123!", "Acme", "User", "en-US"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        factory.SenderMock.Verify(sender => sender.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EndpointWithoutAttribute_ShouldNotRunTurnstile()
    {
        await using var factory = new AuthWebApplicationFactory();
        factory.SenderMock
            .Setup(sender => sender.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationTokenResponse(
                AccessToken: "access",
                AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(5),
                RefreshToken: "refresh",
                RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(1),
                UserId: Guid.NewGuid(),
                UserName: "user",
                Email: "user@norge360.com",
                SessionId: Guid.NewGuid()));

        using var patchedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITurnstileVerifier>();
                services.AddScoped<ITurnstileVerifier, AlwaysFailingTurnstileVerifier>();
            });
        });

        var client = patchedFactory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(Guid.NewGuid(), "refresh"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.SenderMock.Verify(sender => sender.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Login_And_Register_ShouldBeProtectedByAttribute()
    {
        var loginMethod = typeof(AuthController).GetMethod("Login");
        var registerMethod = typeof(AuthController).GetMethod("Register");

        Assert.NotNull(loginMethod);
        Assert.NotNull(registerMethod);
        Assert.NotNull(loginMethod!.GetCustomAttribute<RequireTurnstileAttribute>());
        Assert.NotNull(registerMethod!.GetCustomAttribute<RequireTurnstileAttribute>());
    }

    [Fact]
    public void Controller_ShouldNotDependOnManualTurnstileVerifier()
    {
        var constructor = typeof(AuthController).GetConstructors().Single();
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.DoesNotContain(typeof(ITurnstileVerifier), parameterTypes);
    }

    [Fact]
    public async Task DevelopmentHostname_127001_ShouldBeAccepted()
    {
        var verifier = CreateVerifier(
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = "secret",
                AllowedHostnames = ["norge360.com"]
            },
            new FixedEnvironment("Development"),
            """{"success":true,"hostname":"127.0.0.1","error-codes":[]}""");

        var result = await verifier.VerifyAsync("token", "127.0.0.1", CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ProductionValidation_ShouldFail_WhenEnabledFalse()
    {
        var validation = new TurnstileOptionsValidation(new FixedEnvironment("Production"));
        var result = validation.Validate(
            name: null,
            new TurnstileOptions
            {
                Enabled = false,
                SecretKey = "secret",
                AllowedHostnames = ["norge360.com"]
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public void ProductionValidation_ShouldFail_WhenSecretMissing()
    {
        var validation = new TurnstileOptionsValidation(new FixedEnvironment("Production"));
        var result = validation.Validate(
            name: null,
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = string.Empty,
                AllowedHostnames = ["norge360.com"]
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public async Task DevelopmentSecretMissing_ShouldFailClosed_WhenEnabled()
    {
        var verifier = CreateVerifier(
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = string.Empty,
                AllowedHostnames = ["localhost", "127.0.0.1"]
            },
            new FixedEnvironment("Development"),
            """{"success":true,"hostname":"localhost","error-codes":[]}""");

        var result = await verifier.VerifyAsync("token", "127.0.0.1", CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("turnstile_secret_missing", result.ErrorCode);
    }

    [Fact]
    public void ProductionValidation_ShouldFail_WhenAllowedHostnamesContainLocalhost()
    {
        var validation = new TurnstileOptionsValidation(new FixedEnvironment("Production"));
        var result = validation.Validate(
            name: null,
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = "real-secret",
                AllowedHostnames = ["localhost", "norge360.com"]
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public void ProductionValidation_ShouldFail_WhenAllowedHostnamesContainLoopbackIp()
    {
        var validation = new TurnstileOptionsValidation(new FixedEnvironment("Production"));
        var result = validation.Validate(
            name: null,
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = "real-secret",
                AllowedHostnames = ["127.0.0.1", "auth.norge360.com"]
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public void ProductionValidation_ShouldFail_WhenExpectedHostnamesMissing()
    {
        var validation = new TurnstileOptionsValidation(new FixedEnvironment("Production"));
        var result = validation.Validate(
            name: null,
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = "real-secret",
                AllowedHostnames = ["example.com"]
            });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("1x00000000000000000000AA")]
    [InlineData("2x00000000000000000000AB")]
    [InlineData("3x00000000000000000000FF")]
    public void ProductionValidation_ShouldFail_WhenKnownCloudflareTestSecretIsUsed(string testSecret)
    {
        var validation = new TurnstileOptionsValidation(new FixedEnvironment("Production"));
        var result = validation.Validate(
            name: null,
            new TurnstileOptions
            {
                Enabled = true,
                SecretKey = testSecret,
                AllowedHostnames = ["norge360.com"]
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public async Task RequireTurnstile_WithMissingContract_ShouldFailClosed_WithClearErrorCode()
    {
        var actionContext = new ActionContext
        {
            ActionDescriptor = new ControllerActionDescriptor
            {
                EndpointMetadata = [new RequireTurnstileAttribute()]
            },
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData()
        };
        var context = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: new object());
        var filter = new TurnstileValidationFilter(new AlwaysSuccessfulTurnstileVerifier());
        ActionExecutedContext? executedContext = null;

        await filter.OnActionExecutionAsync(context, () =>
        {
            executedContext = new ActionExecutedContext(actionContext, [], controller: new object());
            return Task.FromResult(executedContext);
        });

        Assert.Null(executedContext);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("turnstile_contract_missing", problem.Extensions["errorCode"]);
    }

    private static CloudflareTurnstileVerifier CreateVerifier(
        TurnstileOptions options,
        IHostEnvironment environment,
        string responseJson)
    {
        var factory = new StaticHttpClientFactory(
            new HttpClient(new StaticHttpMessageHandler(responseJson))
            {
                Timeout = TimeSpan.FromSeconds(2)
            });

        return new CloudflareTurnstileVerifier(
            factory,
            Microsoft.Extensions.Options.Options.Create(options),
            environment,
            Mock.Of<ILogger<CloudflareTurnstileVerifier>>());
    }

    private sealed class AlwaysFailingTurnstileVerifier : ITurnstileVerifier
    {
        public Task<TurnstileVerificationResult> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken)
            => Task.FromResult(TurnstileVerificationResult.Fail("turnstile_validation_failed", "failed"));
    }

    private sealed class AlwaysSuccessfulTurnstileVerifier : ITurnstileVerifier
    {
        public Task<TurnstileVerificationResult> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken)
            => Task.FromResult(TurnstileVerificationResult.Success());
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StaticHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
    }

    private sealed class FixedEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Norge360.Auth.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
