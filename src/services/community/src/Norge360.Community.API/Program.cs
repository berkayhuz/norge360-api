// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Norge360.AspNetCore.CurrentUser;
using Norge360.AspNetCore.TrustedGateway.Abstractions;
using Norge360.AspNetCore.TrustedGateway.Options;
using Norge360.AspNetCore.TrustedGateway.ReplayProtection;
using Norge360.AspNetCore.TrustedGateway.Validation;
using Norge360.Community.API.Middlewares;
using Norge360.Community.API.Security;
using Norge360.Community.Application.DependencyInjection;
using Norge360.Community.Infrastructure.DependencyInjection;
using Norge360.Community.Infrastructure.Initialization;
using Norge360.Community.Infrastructure.Persistence;
using Norge360.CurrentUser;
using Norge360.Media.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddCommunityApplication();
builder.Services.AddCommunityInfrastructure(builder.Configuration);
builder.Services.AddNorge360Media(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var redisConnectionString = builder.Configuration["Infrastructure:DistributedCache:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Norge360:Community:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddOptions<TrustedGatewayOptions>()
    .Bind(builder.Configuration.GetSection("Security:TrustedGateway"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<TrustedGatewayOptions>, CommunityTrustedGatewayOptionsValidation>();
builder.Services.AddSingleton<ITrustedGatewayReplayProtector, DistributedTrustedGatewayReplayProtector>();
builder.Services.AddSingleton<ITrustedGatewayRequestValidator>(sp =>
    new TrustedGatewayRequestValidator(
        sp.GetRequiredService<IOptions<TrustedGatewayOptions>>().Value,
        sp.GetRequiredService<ITrustedGatewayReplayProtector>(),
        sp.GetRequiredService<ILogger<TrustedGatewayRequestValidator>>()));

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

        options.RequireHttpsMetadata = jwtBearer.GetValue("RequireHttpsMetadata", true);
        options.SaveToken = false;

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
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
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
    await dbContext.Database.MigrateAsync();
    var demoCommunitySeeder = scope.ServiceProvider.GetRequiredService<DemoCommunitySeeder>();
    await demoCommunitySeeder.SeedDemoPostsAsync();
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

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
