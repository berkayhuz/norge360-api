// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Norge360.AspNetCore.Health;
using Norge360.Search.API.Endpoints;
using Norge360.Search.API.Security;
using Norge360.Search.Application.DependencyInjection;
using Norge360.Search.Infrastructure.DependencyInjection;
using Norge360.Search.Infrastructure.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSearchApplication();
builder.Services.AddSearchInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Authentication:Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var metadataAddress = jwtSection["MetadataAddress"];
        var authority = jwtSection["Authority"];
        var requireConfiguredJwt = !builder.Environment.IsDevelopment();

        if (requireConfiguredJwt &&
            (string.IsNullOrWhiteSpace(issuer) ||
             string.IsNullOrWhiteSpace(audience) ||
             (string.IsNullOrWhiteSpace(metadataAddress) && string.IsNullOrWhiteSpace(authority))))
        {
            throw new InvalidOperationException("Authentication:Jwt issuer, audience and metadata address or authority must be configured outside Development.");
        }

        options.RequireHttpsMetadata = jwtSection.GetValue("RequireHttpsMetadata", !builder.Environment.IsDevelopment());
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();
        options.SaveToken = false;

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.RefreshOnIssuerKeyNotFound = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            NameClaimType = "name",
            RoleClaimType = "role",
            ValidTypes = ["at+jwt", "JWT"],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<ISearchAccessContextFactory, HttpSearchAccessContextFactory>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Search API process is running."), tags: ["live"])
    .AddCheck<MeilisearchReadinessHealthCheck>("search-provider-ready", tags: ["ready"]);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapSearchEndpoints();

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
