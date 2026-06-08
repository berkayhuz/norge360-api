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
using Norge360.AspNetCore.CurrentUser;
using Norge360.AspNetCore.TrustedGateway.Abstractions;
using Norge360.AspNetCore.TrustedGateway.Options;
using Norge360.AspNetCore.TrustedGateway.ReplayProtection;
using Norge360.AspNetCore.TrustedGateway.Validation;
using Norge360.CurrentUser;
using Norge360.MessagingService.API.Hubs;
using Norge360.MessagingService.API.Middlewares;
using Norge360.MessagingService.API.Security;
using Norge360.MessagingService.Application.Abstractions;
using Norge360.MessagingService.Application.DependencyInjection;
using Norge360.MessagingService.Application.Options;
using Norge360.MessagingService.Infrastructure.DependencyInjection;
using Norge360.MessagingService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddMessagingApplication();
builder.Services.Configure<MessagingRulesOptions>(builder.Configuration.GetSection(MessagingRulesOptions.SectionName));
builder.Services.AddMessagingInfrastructure(builder.Configuration);
builder.Services.AddScoped<IMessagingRealtimePublisher, SignalRMessagingRealtimePublisher>();
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 96 * 1024;
    options.StreamBufferCapacity = 8;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnectionString = builder.Configuration["Infrastructure:DistributedCache:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Norge360:Messaging:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddOptions<TrustedGatewayOptions>()
    .Bind(builder.Configuration.GetSection("Security:TrustedGateway"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<TrustedGatewayOptions>, MessagingTrustedGatewayOptionsValidation>();
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

                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Path.StartsWithSegments("/hubs/messaging") &&
                    context.Request.Query.TryGetValue("access_token", out var accessToken))
                {
                    context.Token = accessToken;
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
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            httpContext.Connection.RemoteIpAddress?.ToString() ??
            "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 240,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
    await dbContext.Database.MigrateAsync();
    await EnsureConversationParticipantNotificationSoundEnabledColumnAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseMiddleware<TrustedGatewayMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<MessagingHub>("/hubs/messaging");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

static async Task EnsureConversationParticipantNotificationSoundEnabledColumnAsync(
    MessagingDbContext dbContext,
    CancellationToken cancellationToken = default)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "MessagingConversationParticipants"
        ADD COLUMN IF NOT EXISTS "NotificationSoundEnabled" boolean NOT NULL DEFAULT true;
        """,
        cancellationToken);
}
