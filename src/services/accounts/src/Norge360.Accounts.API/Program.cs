// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Norge360.Accounts.API.Middlewares;
using Norge360.Accounts.API.Options;
using Norge360.Accounts.API.Security;
using Norge360.Accounts.Application.DependencyInjection;
using Norge360.Accounts.Infrastructure.DependencyInjection;
using Norge360.Accounts.Infrastructure.Initialization;
using Norge360.Accounts.Infrastructure.Persistence;
using Norge360.AspNetCore.CurrentUser;
using Norge360.AspNetCore.TrustedGateway.Abstractions;
using Norge360.AspNetCore.TrustedGateway.Options;
using Norge360.AspNetCore.TrustedGateway.ReplayProtection;
using Norge360.AspNetCore.TrustedGateway.Validation;
using Norge360.CurrentUser;
using Norge360.Media.Storage;

const string UsernameAvailabilityRateLimitPolicyName = "username-availability";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);
builder.Services.AddNorge360Media(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddOptions<TrustedGatewayOptions>()
    .Bind(builder.Configuration.GetSection("Security:TrustedGateway"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<TrustedGatewayOptions>, AccountsTrustedGatewayOptionsValidation>();
builder.Services.AddSingleton<ITrustedGatewayReplayProtector, DistributedTrustedGatewayReplayProtector>();
builder.Services.AddSingleton<ITrustedGatewayRequestValidator>(serviceProvider =>
    new TrustedGatewayRequestValidator(
        serviceProvider.GetRequiredService<IOptions<TrustedGatewayOptions>>().Value,
        serviceProvider.GetRequiredService<ITrustedGatewayReplayProtector>(),
        serviceProvider.GetRequiredService<ILogger<TrustedGatewayRequestValidator>>()));
builder.Services.AddOptions<InternalServiceSigningOptions>()
    .Bind(builder.Configuration.GetSection("InternalServices:Signing"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<InternalServiceSigningOptions>, InternalServiceSigningOptionsValidation>();
builder.Services.AddSingleton<IInternalServiceRequestValidator, HmacInternalServiceRequestValidator>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtBearer = configuration.GetSection("Authentication:JwtBearer");
        var authority = jwtBearer["Authority"] ?? throw new InvalidOperationException("Authentication:JwtBearer:Authority is missing.");
        var metadataAddress = jwtBearer["MetadataAddress"] ?? throw new InvalidOperationException("Authentication:JwtBearer:MetadataAddress is missing.");
        var issuer = jwtBearer["Issuer"] ?? throw new InvalidOperationException("Authentication:JwtBearer:Issuer is missing.");
        var audience = jwtBearer["Audience"] ?? throw new InvalidOperationException("Authentication:JwtBearer:Audience is missing.");
        var accessCookieName = configuration["Security:TokenTransport:AccessCookieName"] ?? "Norge360-access";

        options.Authority = authority;
        options.MetadataAddress = metadataAddress;
        options.RequireHttpsMetadata = jwtBearer.GetValue("RequireHttpsMetadata", true);
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue(accessCookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var subjectClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                                   context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(subjectClaim, out _))
                {
                    context.Fail("JWT subject claim is missing or invalid.");
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(UsernameAvailabilityRateLimitPolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
    await dbContext.Database.MigrateAsync();
    var demoProfileSeeder = scope.ServiceProvider.GetRequiredService<DemoProfileSeeder>();
    await demoProfileSeeder.SeedDemoProfilesAsync();
}

app.UseRouting();
app.UseMiddleware<TrustedGatewayMiddleware>();
app.UseMiddleware<InternalServiceSignatureMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
