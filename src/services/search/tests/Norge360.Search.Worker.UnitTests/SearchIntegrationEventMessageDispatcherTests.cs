// <copyright file="SearchIntegrationEventMessageDispatcherTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Contracts.IntegrationEvents.V1;
using Norge360.Search.Worker.Integration;

namespace Norge360.Search.Worker.UnitTests;

public sealed class SearchIntegrationEventMessageDispatcherTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DispatchAsync_WithIndexRoutingKey_ShouldDeserializeAndCallIngestion()
    {
        var ingestion = new FakeIngestionService();
        var logger = new ListLogger<SearchIntegrationEventMessageDispatcher>();
        var dispatcher = new SearchIntegrationEventMessageDispatcher(ingestion, logger);
        var message = new SearchDocumentIndexRequestedV1(
            Guid.NewGuid(),
            CreateDocument(),
            "corr-1",
            "cause-1",
            DateTime.UtcNow);

        var status = await dispatcher.DispatchAsync("search.index.norge360", JsonSerializer.Serialize(message, JsonOptions), CancellationToken.None);

        status.Should().Be(SearchIntegrationDispatchStatus.Dispatched);
        ingestion.IndexEvents.Should().ContainSingle();
        ingestion.DeleteEvents.Should().BeEmpty();
        ingestion.ReindexEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_WithDeleteRoutingKey_ShouldDeserializeAndCallIngestion()
    {
        var ingestion = new FakeIngestionService();
        var logger = new ListLogger<SearchIntegrationEventMessageDispatcher>();
        var dispatcher = new SearchIntegrationEventMessageDispatcher(ingestion, logger);
        var message = new SearchDocumentDeleteRequestedV1(
            Guid.NewGuid(),
            "doc-1",
            SearchDocumentSource.Forum,
            "module",
            null,
            "corr-1",
            "cause-1",
            DateTime.UtcNow);

        var status = await dispatcher.DispatchAsync("search.delete.norge360", JsonSerializer.Serialize(message, JsonOptions), CancellationToken.None);

        status.Should().Be(SearchIntegrationDispatchStatus.Dispatched);
        ingestion.DeleteEvents.Should().ContainSingle().Which.DocumentId.Should().Be("doc-1");
    }

    [Fact]
    public async Task DispatchAsync_WithReindexRoutingKey_ShouldDeserializeAndCallIngestion()
    {
        var ingestion = new FakeIngestionService();
        var logger = new ListLogger<SearchIntegrationEventMessageDispatcher>();
        var dispatcher = new SearchIntegrationEventMessageDispatcher(ingestion, logger);
        var message = new SearchReindexRequestedV1(
            Guid.NewGuid(),
            SearchDocumentSource.Forum,
            Guid.NewGuid(),
            "user-1",
            "corr-1",
            "cause-1",
            DateTime.UtcNow);

        var status = await dispatcher.DispatchAsync("search.reindex.norge360", JsonSerializer.Serialize(message, JsonOptions), CancellationToken.None);

        status.Should().Be(SearchIntegrationDispatchStatus.Dispatched);
        ingestion.ReindexEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_WithUnknownRoutingKey_ShouldReturnUnsupportedAndNotCallIngestion()
    {
        var ingestion = new FakeIngestionService();
        var logger = new ListLogger<SearchIntegrationEventMessageDispatcher>();
        var dispatcher = new SearchIntegrationEventMessageDispatcher(ingestion, logger);

        var status = await dispatcher.DispatchAsync("search.unknown.norge360", "{}", CancellationToken.None);

        status.Should().Be(SearchIntegrationDispatchStatus.UnsupportedRoutingKey);
        ingestion.IndexEvents.Should().BeEmpty();
        ingestion.DeleteEvents.Should().BeEmpty();
        ingestion.ReindexEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_WithInvalidJson_ShouldReturnInvalidPayloadAndNotCallIngestion()
    {
        var ingestion = new FakeIngestionService();
        var logger = new ListLogger<SearchIntegrationEventMessageDispatcher>();
        var dispatcher = new SearchIntegrationEventMessageDispatcher(ingestion, logger);

        var status = await dispatcher.DispatchAsync("search.index.norge360", "{invalid-json}", CancellationToken.None);

        status.Should().Be(SearchIntegrationDispatchStatus.InvalidPayload);
        ingestion.IndexEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_IndexEventLogsShouldNotContainDocumentContent()
    {
        var ingestion = new FakeIngestionService();
        var logger = new ListLogger<SearchIntegrationEventMessageDispatcher>();
        var dispatcher = new SearchIntegrationEventMessageDispatcher(ingestion, logger);
        const string secretContent = "DO_NOT_LOG_DOCUMENT_CONTENT";

        var message = new SearchDocumentIndexRequestedV1(
            Guid.NewGuid(),
            CreateDocument(content: secretContent),
            "corr-1",
            "cause-1",
            DateTime.UtcNow);

        var status = await dispatcher.DispatchAsync("search.index.norge360", JsonSerializer.Serialize(message, JsonOptions), CancellationToken.None);

        status.Should().Be(SearchIntegrationDispatchStatus.Dispatched);
        logger.Messages.Should().NotContain(messageText => messageText.Contains(secretContent, StringComparison.Ordinal));
    }

    [Fact]
    public void WorkerAssembly_ShouldNotReferenceSearchApiAssembly()
    {
        var references = typeof(SearchIntegrationEventMessageDispatcher).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .ToArray();

        references.Should().NotContain("Norge360.Search.API");
    }

    private static SearchDocument CreateDocument(string content = "Document content") =>
        new(
            Id: "doc-1",
            Source: SearchDocumentSource.Forum,
            Type: "user",
            Title: "User profile",
            Summary: "Public user profile document",
            Content: content,
            Url: "/customers",
            TenantId: null,
            RequiredPermissions: [],
            Visibility: SearchDocumentVisibility.Public,
            Locale: "en",
            Tags: ["users", "profiles"],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            IndexedAtUtc: DateTimeOffset.UtcNow,
            IsDeleted: false,
            Metadata: new Dictionary<string, string>(),
            ExternalId: null,
            OwnerUserId: null,
            AssignedUserIds: [],
            SourceVersion: "v1",
            SourceUpdatedAtUtc: DateTimeOffset.UtcNow,
            PermissionMatchMode: SearchPermissionMatchMode.Any);

    private sealed class FakeIngestionService : ISearchIntegrationEventIngestionService
    {
        public List<SearchDocumentIndexRequestedV1> IndexEvents { get; } = [];
        public List<SearchDocumentDeleteRequestedV1> DeleteEvents { get; } = [];
        public List<SearchReindexRequestedV1> ReindexEvents { get; } = [];

        public Task HandleAsync(SearchDocumentIndexRequestedV1 integrationEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            IndexEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }

        public Task HandleAsync(SearchDocumentDeleteRequestedV1 integrationEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            DeleteEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }

        public Task HandleAsync(SearchReindexRequestedV1 integrationEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            ReindexEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
