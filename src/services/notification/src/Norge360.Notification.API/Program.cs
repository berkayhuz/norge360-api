// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Norge360.AspNetCore.CurrentUser;
using Norge360.AspNetCore.TrustedGateway.Abstractions;
using Norge360.AspNetCore.TrustedGateway.Options;
using Norge360.AspNetCore.TrustedGateway.ReplayProtection;
using Norge360.AspNetCore.TrustedGateway.Validation;
using Norge360.CurrentUser;
using Norge360.Notification.API.Middlewares;
using Norge360.Notification.API.Security;
using Norge360.Notification.Application.DependencyInjection;
using Norge360.Notification.Infrastructure.DependencyInjection;
using Norge360.Notification.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddOptions<TrustedGatewayOptions>()
    .Bind(builder.Configuration.GetSection("Security:TrustedGateway"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<TrustedGatewayOptions>, NotificationTrustedGatewayOptionsValidation>();
builder.Services.AddSingleton<ITrustedGatewayReplayProtector, DistributedTrustedGatewayReplayProtector>();
builder.Services.AddSingleton<ITrustedGatewayRequestValidator>(serviceProvider =>
    new TrustedGatewayRequestValidator(
        serviceProvider.GetRequiredService<IOptions<TrustedGatewayOptions>>().Value,
        serviceProvider.GetRequiredService<ITrustedGatewayReplayProtector>(),
        serviceProvider.GetRequiredService<ILogger<TrustedGatewayRequestValidator>>()));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtBearer = configuration.GetSection("Authentication:JwtBearer");
        var authority = jwtBearer["Authority"];
        var metadataAddress = jwtBearer["MetadataAddress"];
        var issuer = jwtBearer["Issuer"];
        var audience = jwtBearer["Audience"];
        var accessCookieName = configuration["Security:TokenTransport:AccessCookieName"] ?? "Norge360-access";

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.RequireHttpsMetadata = jwtBearer.GetValue("RequireHttpsMetadata", true);
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
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
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NotificationStartup");
        logger.LogWarning(
            ex,
            "Notification database already contains legacy schema objects. Applying compatibility patch and continuing.");

        await NotificationSchemaCompatibility.EnsureCompatibleAsync(
            dbContext,
            logger,
            CancellationToken.None);
    }
}

app.UseRouting();
app.UseMiddleware<TrustedGatewayMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
