// <copyright file="SearchInfrastructureServiceCollectionExtensions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Meilisearch;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Infrastructure.Abstractions;
using Norge360.Search.Infrastructure.Accounts;
using Norge360.Search.Infrastructure.Meilisearch;
using Norge360.Search.Infrastructure.Meilisearch.Client;
using Norge360.Search.Infrastructure.Meilisearch.Documents;
using Norge360.Search.Infrastructure.Meilisearch.Indexing;
using Norge360.Search.Infrastructure.Meilisearch.Queries;
using Norge360.Search.Infrastructure.Options;
using Norge360.Search.Infrastructure.StaticIndexing;

namespace Norge360.Search.Infrastructure.DependencyInjection;

public static class SearchInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSearchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SearchOptions>()
            .Bind(configuration.GetSection(SearchOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Search:Provider is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.IndexName), "Search:IndexName is required.")
            .Validate(options => options.StaticIndexing.StartupSeedMaxAttempts > 0, "Search:StaticIndexing:StartupSeedMaxAttempts must be greater than zero.")
            .Validate(options => options.StaticIndexing.StartupSeedRetryDelaySeconds >= 0, "Search:StaticIndexing:StartupSeedRetryDelaySeconds cannot be negative.")
            .ValidateOnStart();

        services
            .AddOptions<MeilisearchOptions>()
            .Bind(configuration.GetSection(MeilisearchOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "Meilisearch:Endpoint is required.")
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _), "Meilisearch:Endpoint must be an absolute URI.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Meilisearch:ApiKey is required.")
            .ValidateOnStart();
        services
            .AddOptions<SearchBlockFilterOptions>()
            .Bind(configuration.GetSection(SearchBlockFilterOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.AccountsApiBaseUrl), "Search:BlockFilter:AccountsApiBaseUrl is required.")
            .Validate(options => Uri.TryCreate(options.AccountsApiBaseUrl, UriKind.Absolute, out _), "Search:BlockFilter:AccountsApiBaseUrl must be an absolute URI.")
            .ValidateOnStart();

        services.AddHttpClient(MeilisearchOptions.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient(nameof(AccountsBlockedProfileIdsProvider), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        var provider = configuration[$"{SearchOptions.SectionName}:{nameof(SearchOptions.Provider)}"];
        if (provider is not null && provider.Equals("Meilisearch", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MeilisearchOptions>>().Value;
                return new MeilisearchClient(options.Endpoint, options.ApiKey);
            });

            services.AddSingleton<IMeilisearchDocumentClient, MeilisearchDocumentClient>();
            services.AddSingleton<MeilisearchFilterBuilder>();
            services.AddSingleton<MeilisearchDocumentMapper>();
            services.AddSingleton<IMeilisearchIndexInitializer, MeilisearchIndexInitializer>();
            services.AddScoped<IBlockedProfileIdsProvider, AccountsBlockedProfileIdsProvider>();
            services.AddScoped<ISearchQueryService, MeilisearchSearchQueryService>();
            services.AddScoped<ISearchIndexingService, MeilisearchSearchIndexingService>();
            services.AddHostedService<StaticSearchIndexingHostedService>();
        }

        return services;
    }
}
