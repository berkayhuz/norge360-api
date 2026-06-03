// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Messaging.RabbitMq.DependencyInjection;
using Norge360.Search.Application.DependencyInjection;
using Norge360.Search.Infrastructure.DependencyInjection;
using Norge360.Search.Worker;
using Norge360.Search.Worker.Integration;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .AddOptions<SearchIntegrationConsumerOptions>()
    .BindConfiguration(SearchIntegrationConsumerOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSearchApplication();
builder.Services.AddSearchInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<SearchIntegrationEventMessageDispatcher>();
builder.Services.AddHostedService<SearchIntegrationEventConsumer>();

await builder.Build().RunAsync();
