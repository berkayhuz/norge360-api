// <copyright file="StaticSearchIndexingHostedService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Norge360.Search.Application.Indexing.Commands;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Infrastructure.StaticIndexing;

internal sealed class StaticSearchIndexingHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SearchOptions> searchOptions,
    ILogger<StaticSearchIndexingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = searchOptions.Value;
        var config = options.StaticIndexing;
        if (!config.Enabled || !config.SeedOnStartup)
        {
            logger.LogInformation(
                "Static search indexing startup seed is disabled (Search:StaticIndexing:Enabled={Enabled}, SeedOnStartup={SeedOnStartup}).",
                config.Enabled,
                config.SeedOnStartup);
            return;
        }

        var maxAttempts = config.StartupSeedMaxAttempts <= 0 ? 1 : config.StartupSeedMaxAttempts;
        var retryDelay = TimeSpan.FromSeconds(Math.Max(0, config.StartupSeedRetryDelaySeconds));

        logger.LogInformation(
            "Static search startup seed started for index '{IndexName}' with up to {MaxAttempts} attempt(s).",
            options.IndexName,
            maxAttempts);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var seededCount = await mediator.Send(new SeedStaticSearchDocumentsCommand(), stoppingToken);
                logger.LogInformation(
                    "Static search startup seed succeeded for index '{IndexName}' on attempt {Attempt}/{MaxAttempts} with {SeededCount} document(s).",
                    options.IndexName,
                    attempt,
                    maxAttempts,
                    seededCount);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Static search document startup seed was canceled.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Static search startup seed attempt {Attempt}/{MaxAttempts} failed for index '{IndexName}'. Retrying in {RetryDelaySeconds} second(s).",
                    attempt,
                    maxAttempts,
                    options.IndexName,
                    retryDelay.TotalSeconds);

                if (retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(retryDelay, stoppingToken);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Static search startup seed failed after {MaxAttempts} attempt(s) for index '{IndexName}'.",
                    maxAttempts,
                    options.IndexName);
                return;
            }
        }
    }
}
