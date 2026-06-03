// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Norge360.AspNetCore.Health;
using Norge360.Auth.API.Accessors;
using Norge360.Auth.API.Cookies;
using Norge360.Auth.API.Health;
using Norge360.Auth.API.Security.Turnstile;
using Norge360.Auth.Application.DependencyInjection;
using Norge360.Auth.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TurnstileValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<TurnstileValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AuthCookieService>();
builder.Services.AddScoped<AuthRequestContextAccessor>();
builder.Services.AddScoped<ITurnstileVerifier, CloudflareTurnstileVerifier>();
builder.Services.AddHttpClient(nameof(CloudflareTurnstileVerifier), client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddOptions<TurnstileOptions>()
    .Bind(builder.Configuration.GetSection(TurnstileOptions.SectionName))
    .ValidateOnStart();
builder.Services.PostConfigure<TurnstileOptions>(options =>
{
    var envSecret = Environment.GetEnvironmentVariable("CLOUDFLARE_TURNSTILE_SECRET_KEY");
    if (!string.IsNullOrWhiteSpace(envSecret))
    {
        options.SecretKey = envSecret;
    }
});
builder.Services.AddSingleton<IValidateOptions<TurnstileOptions>, TurnstileOptionsValidation>();
builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Auth process is running."), tags: ["live"])
    .AddCheck<AuthDatabaseHealthCheck>("auth-database", tags: ["ready"])
    .AddCheck<AuthPendingMigrationsHealthCheck>("auth-pending-migrations", tags: ["ready", "startup"])
    .AddCheck<DistributedCacheAvailabilityHealthCheck>("auth-cache", tags: ["ready"])
    .AddCheck<JwtSigningKeyHealthCheck>("auth-jwt-signing", tags: ["ready"])
    .AddCheck<TrustedGatewayConfigurationHealthCheck>("auth-trusted-gateway", tags: ["ready", "startup"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks(
        "/health/live",
        HealthResponseWriter.CreateMinimalOptions(registration => registration.Tags.Contains("live")))
    .AllowAnonymous();
app.MapHealthChecks(
        "/health/ready",
        HealthResponseWriter.CreateMinimalOptions(registration => registration.Tags.Contains("ready")))
    .AllowAnonymous();
await app.RunAsync();

public partial class Program;
