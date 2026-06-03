// <copyright file="DiscoveryApplicationDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Application.Services;

namespace Norge360.Discovery.Application.DependencyInjection;

public static class DiscoveryApplicationDependencyInjection
{
    public static IServiceCollection AddDiscoveryApplication(this IServiceCollection services)
    {
        services.AddScoped<IDiscoveryEventIngestionService, DiscoveryEventIngestionService>();
        services.AddScoped<IDiscoveryRankingService, DiscoveryRankingService>();
        services.AddScoped<IDiscoverySnapshotService, DiscoverySnapshotService>();
        return services;
    }
}
