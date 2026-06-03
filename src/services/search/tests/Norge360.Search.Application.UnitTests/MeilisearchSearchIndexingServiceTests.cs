// <copyright file="MeilisearchSearchIndexingServiceTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Options;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Infrastructure.Meilisearch.Client;
using Norge360.Search.Infrastructure.Meilisearch.Documents;
using Norge360.Search.Infrastructure.Meilisearch.Indexing;
using Norge360.Search.Infrastructure.Options;

namespace Norge360.Search.Application.UnitTests;

public sealed class MeilisearchSearchIndexingServiceTests
{
    [Fact]
    public async Task UpsertAsync_ShouldRejectCrmPublicDocument()
    {
        var (service, client, _) = CreateService();
        var document = CreateDocument(source: SearchDocumentSource.Crm, visibility: SearchDocumentVisibility.Public);

        var action = () => service.UpsertAsync(document, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        client.UpsertCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpsertAsync_ShouldRejectAccountPublicDocument()
    {
        var (service, client, _) = CreateService();
        var document = CreateDocument(source: SearchDocumentSource.Account, visibility: SearchDocumentVisibility.Public);

        var action = () => service.UpsertAsync(document, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        client.UpsertCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpsertAsync_ShouldRejectPermissionVisibilityWithoutRequiredPermissions()
    {
        var (service, client, _) = CreateService();
        var document = CreateDocument(
            source: SearchDocumentSource.Tools,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: []);

        var action = () => service.UpsertAsync(document, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        client.UpsertCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpsertAsync_ShouldMapAndSendValidPublicDocument()
    {
        var (service, client, initializer) = CreateService();
        var document = CreateDocument(
            source: SearchDocumentSource.Public,
            visibility: SearchDocumentVisibility.Public,
            content: "searchable content");

        await service.UpsertAsync(document, CancellationToken.None);

        initializer.EnsureCalls.Should().Be(1);
        client.UpsertCalls.Should().Be(1);
        client.LastUpsertedDocument.Should().NotBeNull();
        client.LastUpsertedDocument!.Source.Should().Be("Public");
        client.LastUpsertedDocument.Visibility.Should().Be("Public");
        client.LastUpsertedDocument.Content.Should().Be("searchable content");
    }

    [Fact]
    public async Task UpsertAsync_ShouldMapAndSendValidPermissionDocument()
    {
        var (service, client, _) = CreateService();
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customers.read"],
            permissionMatchMode: SearchPermissionMatchMode.All);

        await service.UpsertAsync(document, CancellationToken.None);

        client.UpsertCalls.Should().Be(1);
        client.LastUpsertedDocument.Should().NotBeNull();
        client.LastUpsertedDocument!.RequiredPermissions.Should().BeEquivalentTo(["crm.customers.read"]);
        client.LastUpsertedDocument.PermissionMatchMode.Should().Be("All");
    }

    [Fact]
    public async Task UpsertManyAsync_ShouldValidateAllDocumentsBeforeWriting()
    {
        var (service, client, _) = CreateService();
        var valid = CreateDocument(source: SearchDocumentSource.Public, visibility: SearchDocumentVisibility.Public);
        var invalid = CreateDocument(source: SearchDocumentSource.Crm, visibility: SearchDocumentVisibility.Public);

        var action = () => service.UpsertManyAsync([valid, invalid], CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        client.UpsertManyCalls.Should().Be(0);
        client.UpsertCalls.Should().Be(0);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldMarkDocumentDeletedAndUpsert()
    {
        var (service, client, _) = CreateService();
        client.StoredDocumentToReturn = new MeilisearchSearchDocument
        {
            Id = "doc-1",
            Source = "Tools",
            Type = "page",
            Title = "Doc",
            Summary = "Summary",
            Content = "Content",
            Url = "/doc",
            Visibility = "Authenticated",
            PermissionMatchMode = "Any",
            Locale = "en-US",
            Tags = ["tag"],
            IsDeleted = false,
            IndexedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        await service.SoftDeleteAsync("doc-1", CancellationToken.None);

        client.UpsertCalls.Should().Be(1);
        client.LastUpsertedDocument.Should().NotBeNull();
        client.LastUpsertedDocument!.IsDeleted.Should().BeTrue();
        client.LastUpsertedDocument.IndexedAtUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task HardDeleteAsync_ShouldCallProviderDelete()
    {
        var (service, client, initializer) = CreateService();

        await service.HardDeleteAsync("doc-1", CancellationToken.None);

        initializer.EnsureCalls.Should().Be(1);
        client.HardDeleteCalls.Should().Be(1);
        client.LastHardDeletedDocumentId.Should().Be("doc-1");
    }

    [Fact]
    public async Task UpsertAsync_ShouldSetIndexedAtUtcWhenMissing()
    {
        var (service, client, _) = CreateService();
        var document = CreateDocument(
            source: SearchDocumentSource.Public,
            visibility: SearchDocumentVisibility.Public,
            indexedAtUtc: DateTimeOffset.MinValue);

        await service.UpsertAsync(document, CancellationToken.None);

        client.LastUpsertedDocument.Should().NotBeNull();
        client.LastUpsertedDocument!.IndexedAtUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenDocumentDoesNotExist_ShouldNotUpsert()
    {
        var (service, client, _) = CreateService();
        client.StoredDocumentToReturn = null;

        await service.SoftDeleteAsync("missing-doc", CancellationToken.None);

        client.UpsertCalls.Should().Be(0);
    }

    private static (
        MeilisearchSearchIndexingService Service,
        FakeDocumentClient Client,
        FakeIndexInitializer Initializer) CreateService()
    {
        var options = Options.Create(new SearchOptions
        {
            Provider = "Meilisearch",
            IndexName = "searchdocuments"
        });

        var client = new FakeDocumentClient();
        var initializer = new FakeIndexInitializer();
        var service = new MeilisearchSearchIndexingService(
            options,
            initializer,
            client,
            new MeilisearchDocumentMapper());

        return (service, client, initializer);
    }

    private static SearchDocument CreateDocument(
        SearchDocumentSource source,
        SearchDocumentVisibility visibility,
        IReadOnlyCollection<string>? requiredPermissions = null,
        SearchPermissionMatchMode permissionMatchMode = SearchPermissionMatchMode.Any,
        string content = "Content",
        DateTimeOffset? indexedAtUtc = null)
    {
        return new SearchDocument(
            Id: Guid.NewGuid().ToString("N"),
            Source: source,
            Type: "page",
            Title: "Title",
            Summary: "Summary",
            Content: content,
            Url: "/path",
            TenantId: null,
            RequiredPermissions: requiredPermissions ?? [],
            Visibility: visibility,
            Locale: "en-US",
            Tags: ["tag"],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-3),
            UpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
            IndexedAtUtc: indexedAtUtc ?? DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted: false,
            Metadata: new Dictionary<string, string>(),
            ExternalId: null,
            OwnerUserId: null,
            AssignedUserIds: [],
            SourceVersion: "v1",
            SourceUpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-4),
            PermissionMatchMode: permissionMatchMode);
    }

    private sealed class FakeDocumentClient : IMeilisearchDocumentClient
    {
        public int UpsertCalls { get; private set; }
        public int UpsertManyCalls { get; private set; }
        public int HardDeleteCalls { get; private set; }
        public string? LastHardDeletedDocumentId { get; private set; }
        public MeilisearchSearchDocument? LastUpsertedDocument { get; private set; }
        public IReadOnlyCollection<MeilisearchSearchDocument> LastBulkUpsertDocuments { get; private set; } = [];
        public MeilisearchSearchDocument? StoredDocumentToReturn { get; set; }

        public Task EnsureIndexExistsAsync(string indexName, string primaryKey, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ApplyIndexConfigurationAsync(string indexName, MeilisearchIndexConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<MeilisearchDocumentSearchPage> SearchAsync(MeilisearchDocumentSearchRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new MeilisearchDocumentSearchPage([], 0));

        public Task UpsertAsync(string indexName, MeilisearchSearchDocument document, CancellationToken cancellationToken)
        {
            UpsertCalls++;
            LastUpsertedDocument = document;
            return Task.CompletedTask;
        }

        public Task UpsertManyAsync(string indexName, IReadOnlyCollection<MeilisearchSearchDocument> documents, CancellationToken cancellationToken)
        {
            UpsertManyCalls++;
            LastBulkUpsertDocuments = documents;
            return Task.CompletedTask;
        }

        public Task<MeilisearchSearchDocument?> GetDocumentAsync(string indexName, string documentId, CancellationToken cancellationToken) =>
            Task.FromResult(StoredDocumentToReturn);

        public Task HardDeleteAsync(string indexName, string documentId, CancellationToken cancellationToken)
        {
            HardDeleteCalls++;
            LastHardDeletedDocumentId = documentId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeIndexInitializer : IMeilisearchIndexInitializer
    {
        public int EnsureCalls { get; private set; }

        public Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            EnsureCalls++;
            return Task.CompletedTask;
        }
    }
}
