// <copyright file="MediaOptimizationPlaceholderService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Hosting;
namespace Norge360.Community.Worker.HostedServices; public sealed class MediaOptimizationPlaceholderService : BackgroundService { protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask; }
