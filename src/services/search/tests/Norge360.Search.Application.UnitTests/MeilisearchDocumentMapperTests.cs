// <copyright file="MeilisearchDocumentMapperTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Search.Application.Queries;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Infrastructure.Meilisearch.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class MeilisearchDocumentMapperTests
{
    private readonly MeilisearchDocumentMapper _mapper = new();

    [Fact]
    public void ToStoredDocument_ShouldPreserveSecurityFields()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            tenantId: tenantId,
            requiredPermissions: ["crm.read", "crm.manage"],
            ownerUserId: ownerUserId,
            assignedUserIds: [assignedUserId],
            permissionMatchMode: SearchPermissionMatchMode.All);

        var stored = _mapper.ToStoredDocument(document);

        stored.Source.Should().Be("Crm");
        stored.Visibility.Should().Be("Permission");
        stored.TenantId.Should().Be(tenantId);
        stored.RequiredPermissions.Should().BeEquivalentTo(["crm.read", "crm.manage"]);
        stored.OwnerUserId.Should().Be(ownerUserId);
        stored.AssignedUserIds.Should().BeEquivalentTo([assignedUserId]);
        stored.PermissionMatchMode.Should().Be("All");
    }

    [Fact]
    public void ToSearchDocument_ShouldMapStoredDocumentBackToContract()
    {
        var stored = new MeilisearchSearchDocument
        {
            Id = "doc-42",
            Source = "Account",
            Type = "settings",
            Title = "Account security",
            Summary = "Security settings",
            Content = "internal content",
            Url = "/account/security",
            TenantId = Guid.NewGuid(),
            RequiredPermissions = ["account.security.read"],
            Visibility = "Permission",
            PermissionMatchMode = "Any",
            Locale = "en-US",
            Tags = ["account", "security"],
            Boost = 1.5,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-3),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2),
            IndexedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted = false,
            Metadata = new Dictionary<string, string> { ["module"] = "account" },
            ExternalId = "ext-42",
            OwnerUserId = Guid.NewGuid(),
            AssignedUserIds = [Guid.NewGuid()],
            SourceVersion = "v3",
            SourceUpdatedAtUtc = DateTimeOffset.UtcNow.AddDays(-4)
        };

        var contract = _mapper.ToSearchDocument(stored);

        contract.Id.Should().Be(stored.Id);
        contract.Source.Should().Be(SearchDocumentSource.Account);
        contract.Visibility.Should().Be(SearchDocumentVisibility.Permission);
        contract.PermissionMatchMode.Should().Be(SearchPermissionMatchMode.Any);
        contract.RequiredPermissions.Should().BeEquivalentTo(stored.RequiredPermissions);
        contract.TenantId.Should().Be(stored.TenantId);
        contract.OwnerUserId.Should().Be(stored.OwnerUserId);
        contract.AssignedUserIds.Should().BeEquivalentTo(stored.AssignedUserIds);
    }

    [Fact]
    public void ToSearchResultItem_ShouldNotExposeContent()
    {
        var stored = new MeilisearchSearchDocument
        {
            Id = "doc-1",
            Source = "Public",
            Type = "page",
            Title = "Docs",
            Summary = "Public docs",
            Content = "This should stay indexed-only",
            Url = "/docs",
            Visibility = "Public",
            PermissionMatchMode = "Any",
            Locale = "en-US",
            Tags = ["docs"],
            Metadata = new Dictionary<string, string>
            {
                ["username"] = "berkay",
                ["displayName"] = "Berkay Huz",
                ["avatarUrl"] = "https://cdn.norge360.com/u/berkay.jpg",
                ["bio"] = "Metal sanatcisi ve yazilimci",
                ["isVerified"] = "true",
                ["followersCount"] = "1200"
            },
            RankingScore = 0.87
        };

        var result = _mapper.ToSearchResultItem(stored);

        result.Id.Should().Be(stored.Id);
        result.Title.Should().Be(stored.Title);
        result.Summary.Should().Be(stored.Summary);
        result.RankingScore.Should().Be(0.87);
        result.Username.Should().Be("berkay");
        result.DisplayName.Should().Be("Berkay Huz");
        result.AvatarUrl.Should().Be("https://cdn.norge360.com/u/berkay.jpg");
        result.Bio.Should().Be("Metal sanatcisi ve yazilimci");
        result.IsVerified.Should().BeTrue();
        result.FollowersCount.Should().Be(1200);
        typeof(SearchResultItem).GetProperty("Content").Should().BeNull();
    }

    private static SearchDocument CreateDocument(
        SearchDocumentSource source,
        SearchDocumentVisibility visibility,
        Guid? tenantId,
        IReadOnlyCollection<string> requiredPermissions,
        Guid? ownerUserId,
        IReadOnlyCollection<Guid> assignedUserIds,
        SearchPermissionMatchMode permissionMatchMode)
    {
        return new SearchDocument(
            Id: "doc-1",
            Source: source,
            Type: "record",
            Title: "Title",
            Summary: "Summary",
            Content: "Content",
            Url: "/internal",
            TenantId: tenantId,
            RequiredPermissions: requiredPermissions,
            Visibility: visibility,
            Locale: "en-US",
            Tags: ["core"],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-3),
            UpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
            IndexedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted: false,
            Metadata: new Dictionary<string, string> { ["k"] = "v" },
            ExternalId: "ext-1",
            OwnerUserId: ownerUserId,
            AssignedUserIds: assignedUserIds,
            SourceVersion: "v1",
            SourceUpdatedAtUtc: DateTimeOffset.UtcNow.AddDays(-4),
            PermissionMatchMode: permissionMatchMode);
    }
}
