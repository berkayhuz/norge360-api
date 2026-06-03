// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Accounts.Application.DependencyInjection;
using Norge360.Accounts.Application.Options;
using Norge360.Accounts.Infrastructure.DependencyInjection;
using Norge360.Accounts.Infrastructure.Initialization;
using Norge360.Accounts.Infrastructure.Options;
using Norge360.Accounts.Worker.Infrastructure;
using Norge360.Accounts.Worker.Integration;
using Norge360.Accounts.Worker.Options;
using Norge360.Media.Abstractions;
using Norge360.Messaging.RabbitMq.DependencyInjection;

var runReservedUsernameSeed = args.Any(IsReservedUsernameSeedCommand);
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = false;
    options.ValidateScopes = false;
});

builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IMediaUploadUrlSigner, NoOpMediaUploadUrlSigner>();
builder.Services.AddSingleton<IMediaStorageProvider, NoOpMediaStorageProvider>();

if (!runReservedUsernameSeed)
{
    builder.Services
        .AddOptions<OutboxOptions>()
        .Bind(builder.Configuration.GetSection(OutboxOptions.SectionName))
        .ValidateOnStart();

    builder.Services
        .AddOptions<AccountsIntegrationOptions>()
        .Bind(builder.Configuration.GetSection(AccountsIntegrationOptions.SectionName))
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<AccountsIntegrationOptions>, AccountsIntegrationOptionsValidator>();
    builder.Services.AddRabbitMqMessaging(builder.Configuration);
    builder.Services.AddSingleton<IAccountsRabbitMqTopologyInitializer, AccountsRabbitMqTopologyInitializer>();
    builder.Services.AddScoped<IUserRegisteredMessageProcessor, UserRegisteredMessageProcessor>();
    builder.Services.AddScoped<OutboxMessagePublisher>();
    builder.Services.AddHostedService<UserRegisteredConsumerService>();
    builder.Services.AddHostedService<OutboxPublisherService>();
    builder.Services
        .AddOptions<SearchBootstrapOptions>()
        .Bind(builder.Configuration.GetSection(SearchBootstrapOptions.SectionName))
        .ValidateOnStart();
    builder.Services.AddHostedService<SearchBootstrapHostedService>();
}

var app = builder.Build();

if (runReservedUsernameSeed)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ReservedUsernameSeedCommand");
    try
    {
        logger.LogInformation("Reserved username seed command started.");
        var seedOptions = app.Services.GetRequiredService<IOptions<ReservedUsernameSeedOptions>>().Value;
        if (seedOptions.ReservedUsernames.Count == 0)
        {
            logger.LogError("Reserved username seed command requires at least one configured reserved username.");
            return 1;
        }

        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IReservedUsernameInitializer>();
        await initializer.InitializeAsync();
        logger.LogInformation(
            "Reserved username seed command completed. ConfiguredReservedUsernameCount={ReservedUsernameCount}",
            seedOptions.ReservedUsernames.Count);
        return 0;
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Reserved username seed command failed.");
        return 1;
    }
}

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
await app.RunAsync();

return 0;

static bool IsReservedUsernameSeedCommand(string value) =>
    string.Equals(value, "--seed-reserved-usernames", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(value, "seed-reserved-usernames", StringComparison.OrdinalIgnoreCase);
