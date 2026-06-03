// <copyright file="SeedStaticSearchDocumentsCommandHandlerTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using MediatR;
using Norge360.Search.Application.Indexing.Commands;
using Norge360.Search.Application.StaticDocuments;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class SeedStaticSearchDocumentsCommandHandlerTests
{
    [Fact]
    public async Task SeedHandler_ShouldSendAllRegistryDocumentsThroughUpsertCommand()
    {
        var documents = new[]
        {
            CreateDocument("doc-1"),
            CreateDocument("doc-2")
        };

        var registry = new FakeStaticSearchDocumentRegistry(documents);
        var sender = new FakeSender();
        var handler = new SeedStaticSearchDocumentsCommandHandler(registry, sender);

        var seededCount = await handler.Handle(new SeedStaticSearchDocumentsCommand(), CancellationToken.None);

        seededCount.Should().Be(2);
        sender.SentRequests.Should().ContainSingle();
        var sentCommand = sender.SentRequests.Single().Should().BeOfType<UpsertSearchDocumentsCommand>().Subject;
        sentCommand.Documents.Should().BeEquivalentTo(documents);
    }

    [Fact]
    public async Task SeedHandler_WhenRegistryIsEmpty_ShouldNotSendUpsertCommand()
    {
        var registry = new FakeStaticSearchDocumentRegistry([]);
        var sender = new FakeSender();
        var handler = new SeedStaticSearchDocumentsCommandHandler(registry, sender);

        var seededCount = await handler.Handle(new SeedStaticSearchDocumentsCommand(), CancellationToken.None);

        seededCount.Should().Be(0);
        sender.SentRequests.Should().BeEmpty();
    }

    private static SearchDocument CreateDocument(string id) =>
        new(
            Id: id,
            Source: SearchDocumentSource.Public,
            Type: "page",
            Title: $"Title {id}",
            Summary: $"Summary {id}",
            Content: $"Content {id}",
            Url: $"/{id}",
            TenantId: null,
            RequiredPermissions: [],
            Visibility: SearchDocumentVisibility.Public,
            Locale: "en-US",
            Tags: ["seed"],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            IndexedAtUtc: DateTimeOffset.MinValue,
            IsDeleted: false,
            Metadata: new Dictionary<string, string>(),
            PermissionMatchMode: SearchPermissionMatchMode.Any);

    private sealed class FakeStaticSearchDocumentRegistry(IReadOnlyCollection<SearchDocument> documents)
        : IStaticSearchDocumentRegistry
    {
        public Task<IReadOnlyCollection<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(documents);
    }

    private sealed class FakeSender : ISender
    {
        public List<object> SentRequests { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            SentRequests.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return AsyncEnumerable.Empty<TResponse>();
        }

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return AsyncEnumerable.Empty<object?>();
        }
    }
}
