// <copyright file="TrustedInternalSourceAuthorizationHandlerBenchmarks.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Norge360.AspNetCore.TrustedGateway.Options;
using Norge360.Auth.API.Middlewares;
using Norge360.Auth.API.Security;

namespace Norge360.Auth.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class TrustedInternalSourceAuthorizationHandlerBenchmarks
{
    private readonly TrustedInternalSourceRequirement _requirement = new();
    private readonly IHttpContextAccessor _httpContextAccessor = new HttpContextAccessor();
    private TrustedInternalSourceAuthorizationHandler _handler = null!;
    private DefaultHttpContext _allowedContext = null!;
    private DefaultHttpContext _deniedContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        var internalIdentityOptions = Options.Create(new InternalIdentityOptions
        {
            AllowedSources = ["Norge360.ApiGateway"]
        });

        var trustedGatewayOptions = Options.Create(new TrustedGatewayOptions
        {
            RequireTrustedGateway = true,
            SourceHeaderName = "X-Gateway-Source",
            AllowedSources = ["Norge360.ApiGateway"]
        });

        _handler = new TrustedInternalSourceAuthorizationHandler(
            _httpContextAccessor,
            internalIdentityOptions,
            trustedGatewayOptions);

        _allowedContext = CreateHttpContext("Norge360.ApiGateway");
        _deniedContext = CreateHttpContext("Norge360.EvilClient");
    }

    [Benchmark]
    public async Task Allow_WithTrustedGatewaySource()
    {
        _httpContextAccessor.HttpContext = _allowedContext;
        var context = new AuthorizationHandlerContext([_requirement], CreatePrincipal(), null);
        await _handler.HandleAsync(context);
    }

    [Benchmark]
    public async Task Deny_WithUntrustedGatewaySource()
    {
        _httpContextAccessor.HttpContext = _deniedContext;
        var context = new AuthorizationHandlerContext([_requirement], CreatePrincipal(), null);
        await _handler.HandleAsync(context);
    }

    private static DefaultHttpContext CreateHttpContext(string source)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/auth/session-status";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("auth.norge360.com");
        context.Request.Headers.Origin = "https://norge360.com";
        context.Request.Headers["Cookie"] = "Norge360-access=demo";
        context.Items[TrustedGatewayMiddleware.TrustedGatewayValidatedItemName] = true;
        context.Request.Headers["X-Gateway-Source"] = source;
        return context;
    }

    private static ClaimsPrincipal CreatePrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity("bench"));
    }
}
