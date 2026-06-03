// <copyright file="CommunityApplicationDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Services;

namespace Norge360.Community.Application.DependencyInjection;

public static class CommunityApplicationDependencyInjection
{
    public static IServiceCollection AddCommunityApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommunityService, CommunityService>();
        services.AddScoped<ICommunityMediaService, CommunityMediaService>();
        return services;
    }
}
