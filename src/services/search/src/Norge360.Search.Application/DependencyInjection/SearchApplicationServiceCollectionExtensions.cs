// <copyright file="SearchApplicationServiceCollectionExtensions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Application.StaticDocuments;
using Norge360.Search.Application.IntegrationEvents;

namespace Norge360.Search.Application.DependencyInjection;

public static class SearchApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSearchApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SearchApplicationServiceCollectionExtensions).Assembly));
        services.AddSingleton<ISearchStaticTextLocalizer>(_ => SearchStaticTextLocalizer.CreateDefault());
        services.AddSingleton<StaticSearchDocumentFactory>();
        services.AddSingleton<IStaticSearchDocumentRegistry, StaticSearchDocumentRegistry>();
        services.AddScoped<ISearchIntegrationEventIngestionService, SearchIntegrationEventIngestionService>();
        return services;
    }
}
