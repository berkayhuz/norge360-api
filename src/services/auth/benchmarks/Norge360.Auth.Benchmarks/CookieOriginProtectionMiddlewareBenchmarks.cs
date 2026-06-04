// <copyright file="CookieOriginProtectionMiddlewareBenchmarks.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Norge360.AspNetCore.ProblemDetails;
using Norge360.Auth.API.Cookies;
using Norge360.Auth.API.Middlewares;
using Norge360.Auth.API.Security;
using Norge360.Auth.Application.Options;

namespace Norge360.Auth.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class CookieOriginProtectionMiddlewareBenchmarks
{
    private readonly RequestDelegate _next = static _ => Task.CompletedTask;
    private readonly IServiceProvider _serviceProvider = new ServiceCollection()
        .AddProblemDetails()
        .Configure<ProblemDetailsOptions>(_ => { })
        .BuildServiceProvider();
    private CookieOriginProtectionMiddleware _middleware = null!;
    private DefaultHttpContext _allowedContext = null!;
    private DefaultHttpContext _rejectedContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        var corsOptions = Options.Create(new ApiCorsOptions
        {
            AllowedOrigins = ["https://norge360.com"]
        });

        var tokenTransportOptions = Options.Create(new TokenTransportOptions
        {
            Mode = TokenTransportModes.CookiesOnly,
            AccessCookieName = "Norge360-access",
            RefreshCookieName = "Norge360-refresh",
            SessionCookieName = "Norge360-session"
        });

        var cookieService = new AuthCookieService(tokenTransportOptions);
        _middleware = new CookieOriginProtectionMiddleware(
            _next,
            corsOptions,
            tokenTransportOptions,
            cookieService,
            NullLogger<CookieOriginProtectionMiddleware>.Instance);

        _allowedContext = CreateContext("https://norge360.com");
        _rejectedContext = CreateContext("https://evil.example");
    }

    [Benchmark]
    public Task Allow_UnsafeCookieRequest_With_AllowedOrigin()
    {
        return _middleware.InvokeAsync(_allowedContext);
    }

    [Benchmark]
    public Task Reject_UnsafeCookieRequest_With_DisallowedOrigin()
    {
        return _middleware.InvokeAsync(_rejectedContext);
    }

    private DefaultHttpContext CreateContext(string origin)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/auth/login";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("auth.norge360.com");
        context.Request.Headers.Origin = origin;
        context.Request.Headers["Cookie"] = "Norge360-access=demo";
        context.Response.Body = new MemoryStream();
        return context;
    }
}
