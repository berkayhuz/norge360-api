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
await app.RunAsync();
