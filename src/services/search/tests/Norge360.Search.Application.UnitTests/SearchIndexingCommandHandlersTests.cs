// <copyright file="SearchIndexingCommandHandlersTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Search.Application.Abstractions;
using Norge360.Search.Application.Indexing.Commands;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class SearchIndexingCommandHandlersTests
{
    [Fact]
    public async Task UpsertSingleHandler_ShouldCallIndexingService()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new UpsertSearchDocumentCommandHandler(indexingService);
        var document = CreateDocument();

        await handler.Handle(new UpsertSearchDocumentCommand(document), CancellationToken.None);

        indexingService.UpsertCalls.Should().Be(1);
        indexingService.LastUpsertDocument.Should().BeSameAs(document);
    }

    [Fact]
    public async Task UpsertSingleHandler_ShouldRejectNullDocument()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new UpsertSearchDocumentCommandHandler(indexingService);
        var command = new UpsertSearchDocumentCommand(null!);

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentNullException>();
        indexingService.UpsertCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpsertManyHandler_ShouldCallIndexingService()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new UpsertSearchDocumentsCommandHandler(indexingService);
        var documents = new[] { CreateDocument(), CreateDocument() };

        await handler.Handle(new UpsertSearchDocumentsCommand(documents), CancellationToken.None);

        indexingService.UpsertManyCalls.Should().Be(1);
        indexingService.LastUpsertManyDocuments.Should().BeEquivalentTo(documents);
    }

    [Fact]
    public async Task UpsertManyHandler_ShouldRejectNullCollection()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new UpsertSearchDocumentsCommandHandler(indexingService);
        var command = new UpsertSearchDocumentsCommand(null!);

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentNullException>();
        indexingService.UpsertManyCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpsertManyHandler_ShouldRejectEmptyCollection()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new UpsertSearchDocumentsCommandHandler(indexingService);
        var command = new UpsertSearchDocumentsCommand([]);

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        indexingService.UpsertManyCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpsertManyHandler_ShouldRejectCollectionWithNullItem()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new UpsertSearchDocumentsCommandHandler(indexingService);
        SearchDocument?[] docsWithNull = [CreateDocument(), null];
        var command = new UpsertSearchDocumentsCommand(docsWithNull!);

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        indexingService.UpsertManyCalls.Should().Be(0);
    }

    [Fact]
    public async Task SoftDeleteHandler_ShouldRejectEmptyDocumentId()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new SoftDeleteSearchDocumentCommandHandler(indexingService);

        var action = () => handler.Handle(new SoftDeleteSearchDocumentCommand("   "), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        indexingService.SoftDeleteCalls.Should().Be(0);
    }

    [Fact]
    public async Task SoftDeleteHandler_ShouldCallIndexingService()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new SoftDeleteSearchDocumentCommandHandler(indexingService);

        await handler.Handle(new SoftDeleteSearchDocumentCommand("doc-1"), CancellationToken.None);

        indexingService.SoftDeleteCalls.Should().Be(1);
        indexingService.LastSoftDeleteDocumentId.Should().Be("doc-1");
    }

    [Fact]
    public async Task HardDeleteHandler_ShouldRejectEmptyDocumentId()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new HardDeleteSearchDocumentCommandHandler(indexingService);

        var action = () => handler.Handle(new HardDeleteSearchDocumentCommand(string.Empty), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        indexingService.HardDeleteCalls.Should().Be(0);
    }

    [Fact]
    public async Task HardDeleteHandler_ShouldCallIndexingService()
    {
        var indexingService = new FakeSearchIndexingService();
        var handler = new HardDeleteSearchDocumentCommandHandler(indexingService);

        await handler.Handle(new HardDeleteSearchDocumentCommand("doc-2"), CancellationToken.None);

        indexingService.HardDeleteCalls.Should().Be(1);
        indexingService.LastHardDeleteDocumentId.Should().Be("doc-2");
    }

    private static SearchDocument CreateDocument()
    {
        return new SearchDocument(
            Id: Guid.NewGuid().ToString("N"),
            Source: SearchDocumentSource.Public,
            Type: "page",
            Title: "Title",
            Summary: "Summary",
            Content: "Content",
            Url: "/page",
            TenantId: null,
            RequiredPermissions: [],
            Visibility: SearchDocumentVisibility.Public,
            Locale: "en-US",
            Tags: ["tag"],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            IndexedAtUtc: DateTimeOffset.UtcNow,
            IsDeleted: false,
            Metadata: new Dictionary<string, string>(),
            PermissionMatchMode: SearchPermissionMatchMode.Any);
    }

    private sealed class FakeSearchIndexingService : ISearchIndexingService
    {
        public int UpsertCalls { get; private set; }
        public int UpsertManyCalls { get; private set; }
        public int SoftDeleteCalls { get; private set; }
        public int HardDeleteCalls { get; private set; }

        public SearchDocument? LastUpsertDocument { get; private set; }
        public IReadOnlyCollection<SearchDocument> LastUpsertManyDocuments { get; private set; } = [];
        public string? LastSoftDeleteDocumentId { get; private set; }
        public string? LastHardDeleteDocumentId { get; private set; }

        public Task UpsertAsync(SearchDocument document, CancellationToken cancellationToken)
        {
            UpsertCalls++;
            LastUpsertDocument = document;
            return Task.CompletedTask;
        }

        public Task UpsertManyAsync(IReadOnlyCollection<SearchDocument> documents, CancellationToken cancellationToken)
        {
            UpsertManyCalls++;
            LastUpsertManyDocuments = documents;
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(string documentId, CancellationToken cancellationToken)
        {
            SoftDeleteCalls++;
            LastSoftDeleteDocumentId = documentId;
            return Task.CompletedTask;
        }

        public Task HardDeleteAsync(string documentId, CancellationToken cancellationToken)
        {
            HardDeleteCalls++;
            LastHardDeleteDocumentId = documentId;
            return Task.CompletedTask;
        }
    }
}

