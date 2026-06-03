// <copyright file="SearchIntegrationEventIngestionServiceTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Norge360.Search.Application.Indexing.Commands;
using Norge360.Search.Application.IntegrationEvents;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Contracts.IntegrationEvents.V1;

namespace Norge360.Search.Application.UnitTests;

public sealed class SearchIntegrationEventIngestionServiceTests
{
    [Fact]
    public async Task HandleIndexEvent_ShouldMapToUpsertCommandAndPreserveSecurityFields()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customer-management.customers.read"],
            tenantId: Guid.NewGuid());
        var integrationEvent = new SearchDocumentIndexRequestedV1(
            EventId: Guid.NewGuid(),
            Document: document,
            CorrelationId: "corr-1",
            CausationId: "cause-1",
            OccurredAtUtc: DateTime.UtcNow);

        await service.HandleAsync(integrationEvent, CancellationToken.None);

        sender.SentRequests.Should().ContainSingle();
        var command = sender.SentRequests.Single().Should().BeOfType<UpsertSearchDocumentCommand>().Subject;
        command.Document.Id.Should().Be(document.Id);
        command.Document.Source.Should().Be(SearchDocumentSource.Crm);
        command.Document.Visibility.Should().Be(SearchDocumentVisibility.Permission);
        command.Document.RequiredPermissions.Should().ContainSingle("crm.customer-management.customers.read");
        command.Document.TenantId.Should().Be(document.TenantId);
        command.Document.Url.Should().Be(document.Url);
        command.Document.Title.Should().Be(document.Title);
        command.Document.Type.Should().Be(document.Type);
        command.Document.PermissionMatchMode.Should().Be(document.PermissionMatchMode);
    }

    [Fact]
    public async Task HandleIndexEvent_WhenCrmPublic_ShouldFailValidationAndNotSendCommand()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Public);
        var integrationEvent = new SearchDocumentIndexRequestedV1(
            EventId: Guid.NewGuid(),
            Document: document,
            CorrelationId: null,
            CausationId: null,
            OccurredAtUtc: DateTime.UtcNow);

        var action = () => service.HandleAsync(integrationEvent, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        sender.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleIndexEvent_WhenPermissionVisibilityHasNoPermissions_ShouldFailValidation()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: []);
        var integrationEvent = new SearchDocumentIndexRequestedV1(
            EventId: Guid.NewGuid(),
            Document: document,
            CorrelationId: null,
            CausationId: null,
            OccurredAtUtc: DateTime.UtcNow);

        var action = () => service.HandleAsync(integrationEvent, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        sender.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleIndexEvent_WhenRequiredFieldsMissing_ShouldRejectBeforeSend()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var document = CreateDocument(title: "   ");
        var integrationEvent = new SearchDocumentIndexRequestedV1(
            EventId: Guid.NewGuid(),
            Document: document,
            CorrelationId: null,
            CausationId: null,
            OccurredAtUtc: DateTime.UtcNow);

        var action = () => service.HandleAsync(integrationEvent, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        sender.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleDeleteEvent_ShouldSendSoftDeleteCommandByDefault()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var integrationEvent = new SearchDocumentDeleteRequestedV1(
            EventId: Guid.NewGuid(),
            DocumentId: "doc-1",
            Source: SearchDocumentSource.Crm,
            Type: "module",
            TenantId: Guid.NewGuid(),
            CorrelationId: "corr-1",
            CausationId: "cause-1",
            OccurredAtUtc: DateTime.UtcNow);

        await service.HandleAsync(integrationEvent, CancellationToken.None);

        sender.SentRequests.Should().ContainSingle();
        sender.SentRequests.Single().Should().BeOfType<SoftDeleteSearchDocumentCommand>()
            .Which.DocumentId.Should().Be("doc-1");
    }

    [Fact]
    public async Task HandleDeleteEvent_WhenDocumentIdIsEmpty_ShouldReject()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var integrationEvent = new SearchDocumentDeleteRequestedV1(
            EventId: Guid.NewGuid(),
            DocumentId: "  ",
            Source: SearchDocumentSource.Public,
            Type: "page",
            TenantId: null,
            CorrelationId: null,
            CausationId: null,
            OccurredAtUtc: DateTime.UtcNow);

        var action = () => service.HandleAsync(integrationEvent, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        sender.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleReindexEvent_ShouldBeExplicitNoOpAndNotDispatchCommands()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var integrationEvent = new SearchReindexRequestedV1(
            EventId: Guid.NewGuid(),
            Source: SearchDocumentSource.Crm,
            TenantId: Guid.NewGuid(),
            RequestedBy: "user-1",
            CorrelationId: "corr-1",
            CausationId: "cause-1",
            OccurredAtUtc: DateTime.UtcNow);

        await service.HandleAsync(integrationEvent, CancellationToken.None);

        sender.SentRequests.Should().BeEmpty();
    }

    private static SearchIntegrationEventIngestionService CreateService(FakeSender sender) =>
        new(sender, NullLogger<SearchIntegrationEventIngestionService>.Instance);

    private static SearchDocument CreateDocument(
        SearchDocumentSource source = SearchDocumentSource.Public,
        SearchDocumentVisibility visibility = SearchDocumentVisibility.Public,
        IReadOnlyCollection<string>? requiredPermissions = null,
        Guid? tenantId = null,
        string? id = null,
        string type = "page",
        string title = "Title",
        string url = "/page")
    {
        return new SearchDocument(
            Id: id ?? Guid.NewGuid().ToString("N"),
            Source: source,
            Type: type,
            Title: title,
            Summary: "Summary",
            Content: "Content",
            Url: url,
            TenantId: tenantId,
            RequiredPermissions: requiredPermissions ?? [],
            Visibility: visibility,
            Locale: "en",
            Tags: ["tag"],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            IndexedAtUtc: DateTimeOffset.UtcNow,
            IsDeleted: false,
            Metadata: new Dictionary<string, string> { ["key"] = "value" },
            ExternalId: "external-1",
            OwnerUserId: Guid.NewGuid(),
            AssignedUserIds: [Guid.NewGuid()],
            SourceVersion: "v1",
            SourceUpdatedAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            PermissionMatchMode: SearchPermissionMatchMode.Any);
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
