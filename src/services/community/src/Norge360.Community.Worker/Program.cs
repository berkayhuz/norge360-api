// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Application.DependencyInjection;
using Norge360.Community.Infrastructure.DependencyInjection;
using Norge360.Community.Worker.HostedServices;
using Norge360.Community.Worker.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = false;
    options.ValidateScopes = false;
});

builder.Services.AddCommunityApplication();
builder.Services.AddCommunityInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services
    .AddOptions<CommunityMediaCleanupOptions>()
    .Bind(builder.Configuration.GetSection(CommunityMediaCleanupOptions.SectionName));
builder.Services.AddHostedService<OrphanMediaCleanupHostedService>();

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
await app.RunAsync();
