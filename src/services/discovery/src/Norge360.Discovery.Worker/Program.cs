// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Discovery.Application.DependencyInjection;
using Norge360.Discovery.Infrastructure.DependencyInjection;
using Norge360.Discovery.Infrastructure.HealthChecks;
using Norge360.Discovery.Worker.HostedServices;
using Norge360.Discovery.Worker.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = false;
    options.ValidateScopes = false;
});

builder.Services.AddDiscoveryApplication();
builder.Services.AddDiscoveryInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<DiscoveryDbHealthCheck>("discovery-db");
builder.Services.AddOptions<DiscoveryRankingWorkerOptions>().Bind(builder.Configuration.GetSection(DiscoveryRankingWorkerOptions.SectionName));
builder.Services.AddHostedService<DiscoveryRankingHostedService>();

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
await app.RunAsync();
