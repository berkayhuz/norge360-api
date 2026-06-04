// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

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
using Norge360.Discovery.API.Middlewares;
using Norge360.Discovery.API.Options;
using Norge360.Discovery.API.Security;
using Norge360.Discovery.API.Services;
using Norge360.Discovery.Application.DependencyInjection;
using Norge360.Discovery.Infrastructure.DependencyInjection;
using Norge360.Discovery.Infrastructure.HealthChecks;
using Norge360.Discovery.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddDiscoveryApplication();
builder.Services.AddDiscoveryInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddOptions<DiscoveryInternalEventOptions>().Bind(builder.Configuration.GetSection(DiscoveryInternalEventOptions.SectionName));
builder.Services.AddOptions<DiscoveryAccountsOptions>().Bind(builder.Configuration.GetSection(DiscoveryAccountsOptions.SectionName));
builder.Services.AddOptions<TrustedGatewayOptions>().Bind(builder.Configuration.GetSection("Security:TrustedGateway")).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<TrustedGatewayOptions>, DiscoveryTrustedGatewayOptionsValidation>();
builder.Services.AddSingleton<ITrustedGatewayReplayProtector, DistributedTrustedGatewayReplayProtector>();
builder.Services.AddSingleton<ITrustedGatewayRequestValidator>(sp => new TrustedGatewayRequestValidator(sp.GetRequiredService<IOptions<TrustedGatewayOptions>>().Value, sp.GetRequiredService<ITrustedGatewayReplayProtector>(), sp.GetRequiredService<ILogger<TrustedGatewayRequestValidator>>()));
builder.Services.AddScoped<IAccountsDiscoveryBackfillService, AccountsDiscoveryBackfillService>();
builder.Services.AddHttpClient("accounts-discovery-export", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<DiscoveryAccountsOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("Services:Accounts:BaseUrl is missing for Discovery accounts backfill.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IConfiguration>((options, configuration) =>
{
    var jwtBearer = configuration.GetSection("Authentication:JwtBearer");
    options.RequireHttpsMetadata = jwtBearer.GetValue("RequireHttpsMetadata", true);
    options.SaveToken = false;
    options.Authority = RequireJwtBearerSetting(jwtBearer, "Authority");
    options.MetadataAddress = RequireJwtBearerSetting(jwtBearer, "MetadataAddress");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtBearer["Issuer"]),
        ValidIssuer = jwtBearer["Issuer"],
        ValidateAudience = !string.IsNullOrWhiteSpace(jwtBearer["Audience"]),
        ValidAudience = jwtBearer["Audience"],
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessCookieName = configuration["Security:TokenTransport:AccessCookieName"] ?? "Norge360-access";
            if (string.IsNullOrWhiteSpace(context.Token) && context.Request.Cookies.TryGetValue(accessCookieName, out var cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddCheck<DiscoveryDbHealthCheck>("discovery-db");

static string RequireJwtBearerSetting(IConfigurationSection jwtBearer, string key)
{
    var value = jwtBearer[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Authentication:JwtBearer:{key} is required.");
    }

    return value;
}

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var migrateOnStartup = app.Configuration.GetValue("Discovery:Database:MigrateOnStartup", app.Environment.IsDevelopment());
    if (migrateOnStartup)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DiscoveryDbContext>();
        await dbContext.Database.MigrateAsync();
    }
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
