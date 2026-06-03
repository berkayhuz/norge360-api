// <copyright file="OrphanMediaCleanupHostedService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Worker.Options;

namespace Norge360.Community.Worker.HostedServices;

public sealed class OrphanMediaCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CommunityMediaCleanupOptions> options,
    ILogger<OrphanMediaCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.Value.Enabled)
            {
                try
                {
                    await RunCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Community media cleanup failed");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, options.Value.IntervalMinutes)), stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICommunityDbContext>();
        var mediaService = scope.ServiceProvider.GetRequiredService<ICommunityMediaService>();
        var threshold = DateTime.UtcNow.AddHours(-Math.Max(1, options.Value.SoftDeletedOlderThanHours));

        var stale = await db.CommunityPostMedia
            .Where(x => (x.IsDeleted || x.Status == Domain.Enums.CommunityMediaStatus.Failed) && x.UpdatedAt != null && x.UpdatedAt < threshold)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var media in stale)
        {
            _ = await mediaService.DeleteMediaByStorageKeyAsync(media.StorageKey, cancellationToken);
        }
    }
}
