// <copyright file="StaticSearchIndexingHostedServiceTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Norge360.Search.Application.Indexing.Commands;
using Norge360.Search.Infrastructure.Options;
using Norge360.Search.Infrastructure.StaticIndexing;

namespace Norge360.Search.Application.UnitTests;

public sealed class StaticSearchIndexingHostedServiceTests
{
    [Fact]
    public async Task HostedService_ShouldNotSeed_WhenStaticIndexingIsDisabled()
    {
        var mediator = new FakeMediator();
        var scopeFactory = BuildScopeFactory(mediator);
        var options = Options.Create(new SearchOptions
        {
            Provider = "Meilisearch",
            IndexName = "searchdocuments",
            StaticIndexing = new SearchStaticIndexingOptions
            {
                Enabled = false,
                SeedOnStartup = true
            }
        });

        var service = new StaticSearchIndexingHostedService(
            scopeFactory,
            options,
            NullLogger<StaticSearchIndexingHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        mediator.SeedRequests.Should().Be(0);
    }

    [Fact]
    public async Task HostedService_ShouldNotSeed_WhenSeedOnStartupIsDisabled()
    {
        var mediator = new FakeMediator();
        var scopeFactory = BuildScopeFactory(mediator);
        var options = Options.Create(new SearchOptions
        {
            Provider = "Meilisearch",
            IndexName = "searchdocuments",
            StaticIndexing = new SearchStaticIndexingOptions
            {
                Enabled = true,
                SeedOnStartup = false
            }
        });

        var service = new StaticSearchIndexingHostedService(
            scopeFactory,
            options,
            NullLogger<StaticSearchIndexingHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        mediator.SeedRequests.Should().Be(0);
    }

    [Fact]
    public async Task HostedService_ShouldSeed_WhenEnabledAndSeedOnStartupTrue()
    {
        var mediator = new FakeMediator();
        var scopeFactory = BuildScopeFactory(mediator);
        var options = Options.Create(new SearchOptions
        {
            Provider = "Meilisearch",
            IndexName = "searchdocuments",
            StaticIndexing = new SearchStaticIndexingOptions
            {
                Enabled = true,
                SeedOnStartup = true
            }
        });

        var service = new StaticSearchIndexingHostedService(
            scopeFactory,
            options,
            NullLogger<StaticSearchIndexingHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        mediator.SeedRequests.Should().Be(1);
    }

    [Fact]
    public async Task HostedService_ShouldHandleFailuresWithoutThrowing()
    {
        var mediator = new FakeMediator
        {
            ThrowOnSeed = true
        };
        var scopeFactory = BuildScopeFactory(mediator);
        var options = Options.Create(new SearchOptions
        {
            Provider = "Meilisearch",
            IndexName = "searchdocuments",
            StaticIndexing = new SearchStaticIndexingOptions
            {
                Enabled = true,
                SeedOnStartup = true
            }
        });

        var service = new StaticSearchIndexingHostedService(
            scopeFactory,
            options,
            NullLogger<StaticSearchIndexingHostedService>.Instance);

        var action = async () =>
        {
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);
        };

        await action.Should().NotThrowAsync();
        mediator.SeedRequests.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HostedService_ShouldRetryAndEventuallySucceed_WhenTransientFailureOccurs()
    {
        var mediator = new FakeMediator
        {
            FailuresBeforeSuccess = 1
        };
        var scopeFactory = BuildScopeFactory(mediator);
        var options = Options.Create(new SearchOptions
        {
            Provider = "Meilisearch",
            IndexName = "searchdocuments",
            StaticIndexing = new SearchStaticIndexingOptions
            {
                Enabled = true,
                SeedOnStartup = true,
                StartupSeedMaxAttempts = 3,
                StartupSeedRetryDelaySeconds = 0
            }
        });

        var service = new StaticSearchIndexingHostedService(
            scopeFactory,
            options,
            NullLogger<StaticSearchIndexingHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        mediator.SeedRequests.Should().Be(2);
    }

    [Fact]
    public void SearchOptions_DefaultStaticIndexing_ShouldBeDisabled()
    {
        var options = new SearchOptions();

        options.StaticIndexing.Enabled.Should().BeFalse();
        options.StaticIndexing.SeedOnStartup.Should().BeFalse();
    }

    private static IServiceScopeFactory BuildScopeFactory(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mediator);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private sealed class FakeMediator : IMediator
    {
        public int SeedRequests { get; private set; }
        public bool ThrowOnSeed { get; set; }
        public int FailuresBeforeSuccess { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is SeedStaticSearchDocumentsCommand)
            {
                SeedRequests++;
                if (ThrowOnSeed || SeedRequests <= FailuresBeforeSuccess)
                {
                    throw new InvalidOperationException("seed failed");
                }
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            if (request is SeedStaticSearchDocumentsCommand)
            {
                SeedRequests++;
                if (ThrowOnSeed || SeedRequests <= FailuresBeforeSuccess)
                {
                    throw new InvalidOperationException("seed failed");
                }
            }

            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (request is SeedStaticSearchDocumentsCommand)
            {
                SeedRequests++;
                if (ThrowOnSeed || SeedRequests <= FailuresBeforeSuccess)
                {
                    throw new InvalidOperationException("seed failed");
                }
            }

            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<object?>();

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
