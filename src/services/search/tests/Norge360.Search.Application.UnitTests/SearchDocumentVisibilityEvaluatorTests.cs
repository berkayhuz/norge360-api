// <copyright file="SearchDocumentVisibilityEvaluatorTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class SearchDocumentVisibilityEvaluatorTests
{
    [Fact]
    public void AnonymousUser_CanSee_PublicSourcePublicDocument()
    {
        var document = CreateDocument(source: SearchDocumentSource.Public, visibility: SearchDocumentVisibility.Public);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, SearchAccessContext.Anonymous);

        canAccess.Should().BeTrue();
    }

    [Fact]
    public void AnonymousUser_CannotSee_AuthenticatedDocument()
    {
        var document = CreateDocument(visibility: SearchDocumentVisibility.Authenticated);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, SearchAccessContext.Anonymous);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void AnonymousUser_CannotSee_TenantDocument()
    {
        var tenantId = Guid.NewGuid();
        var document = CreateDocument(visibility: SearchDocumentVisibility.Tenant, tenantId: tenantId);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, SearchAccessContext.Anonymous);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void AnonymousUser_CannotSee_PermissionDocument()
    {
        var document = CreateDocument(
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customer-management.customers.read"]);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, SearchAccessContext.Anonymous);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void AnonymousUser_CannotSee_CrmDocument()
    {
        var document = CreateDocument(source: SearchDocumentSource.Crm, visibility: SearchDocumentVisibility.Public);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, SearchAccessContext.Anonymous);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void AnonymousUser_CannotSee_AccountDocument()
    {
        var document = CreateDocument(source: SearchDocumentSource.Account, visibility: SearchDocumentVisibility.Public);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, SearchAccessContext.Anonymous);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void AuthenticatedUser_CanSee_AuthenticatedDocument()
    {
        var document = CreateDocument(visibility: SearchDocumentVisibility.Authenticated);
        var access = new SearchAccessContext(true, null, Guid.NewGuid(), []);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeTrue();
    }

    [Fact]
    public void TenantDocument_IsVisibleOnlyWhenTenantMatches()
    {
        var tenantId = Guid.NewGuid();
        var document = CreateDocument(visibility: SearchDocumentVisibility.Tenant, tenantId: tenantId);

        var matchingAccess = new SearchAccessContext(true, null, tenantId, []);
        var nonMatchingAccess = new SearchAccessContext(true, null, Guid.NewGuid(), []);

        SearchDocumentVisibilityEvaluator.CanAccess(document, matchingAccess).Should().BeTrue();
        SearchDocumentVisibilityEvaluator.CanAccess(document, nonMatchingAccess).Should().BeFalse();
    }

    [Fact]
    public void PermissionDocument_WithAnyMode_IsVisibleWhenUserHasAtLeastOneRequiredPermission()
    {
        var document = CreateDocument(
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions:
            [
                "crm.customer-management.customers.read",
                "crm.customer-management.customers.manage"
            ],
            permissionMatchMode: SearchPermissionMatchMode.Any);

        var access = new SearchAccessContext(true, null, Guid.NewGuid(), ["crm.customer-management.customers.read"]);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeTrue();
    }

    [Fact]
    public void GlobalPermissionDocument_WithoutPermission_ShouldNotBeVisible()
    {
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            tenantId: null,
            requiredPermissions: ["crm.customer-management.customers.read"]);
        var access = new SearchAccessContext(true, null, Guid.NewGuid(), []);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void PermissionDocument_WithAllMode_IsVisibleOnlyWhenUserHasAllRequiredPermissions()
    {
        var document = CreateDocument(
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions:
            [
                "crm.customer-management.customers.read",
                "crm.customer-management.customers.manage"
            ],
            permissionMatchMode: SearchPermissionMatchMode.All);

        var partialAccess = new SearchAccessContext(true, null, Guid.NewGuid(), ["crm.customer-management.customers.read"]);
        var fullAccess = new SearchAccessContext(
            true,
            null,
            Guid.NewGuid(),
            [
                "crm.customer-management.customers.read",
                "crm.customer-management.customers.manage"
            ]);

        SearchDocumentVisibilityEvaluator.CanAccess(document, partialAccess).Should().BeFalse();
        SearchDocumentVisibilityEvaluator.CanAccess(document, fullAccess).Should().BeTrue();
    }

    [Fact]
    public void WildcardPermission_GrantsPermissionVisibilityAccess()
    {
        var document = CreateDocument(
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customer-management.customers.manage"],
            permissionMatchMode: SearchPermissionMatchMode.All);

        var access = new SearchAccessContext(true, null, Guid.NewGuid(), ["*"]);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeTrue();
    }

    [Fact]
    public void PermissionDocument_WithTenantIdMatchingAndPermission_ShouldBeVisible()
    {
        var tenantId = Guid.NewGuid();
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            tenantId: tenantId,
            requiredPermissions: ["crm.customer-management.customers.read"]);
        var access = new SearchAccessContext(true, null, tenantId, ["crm.customer-management.customers.read"]);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeTrue();
    }

    [Fact]
    public void PermissionDocument_WithTenantIdMismatch_ShouldNotBeVisibleEvenWithPermission()
    {
        var documentTenantId = Guid.NewGuid();
        var accessTenantId = Guid.NewGuid();
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            tenantId: documentTenantId,
            requiredPermissions: ["crm.customer-management.customers.read"]);
        var access = new SearchAccessContext(true, null, accessTenantId, ["crm.customer-management.customers.read"]);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeFalse();
    }

    [Fact]
    public void PermissionDocument_WithTenantIdAndWildcardPermission_ShouldStillRequireTenantMatch()
    {
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            tenantId: Guid.NewGuid(),
            requiredPermissions: ["crm.customer-management.customers.read"]);
        var access = new SearchAccessContext(true, null, Guid.NewGuid(), ["*"]);

        var canAccess = SearchDocumentVisibilityEvaluator.CanAccess(document, access);

        canAccess.Should().BeFalse();
    }

    private static SearchDocument CreateDocument(
        SearchDocumentSource source = SearchDocumentSource.Public,
        SearchDocumentVisibility visibility = SearchDocumentVisibility.Public,
        IReadOnlyCollection<string>? requiredPermissions = null,
        Guid? tenantId = null,
        SearchPermissionMatchMode permissionMatchMode = SearchPermissionMatchMode.Any,
        string type = "page") =>
        new(
            Id: "doc-1",
            Source: source,
            Type: type,
            Title: "Title",
            Summary: "Summary",
            Content: "Content",
            Url: "/public/page",
            TenantId: tenantId,
            RequiredPermissions: requiredPermissions ?? [],
            Visibility: visibility,
            Locale: "en-US",
            Tags: [],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            IndexedAtUtc: DateTimeOffset.UtcNow,
            IsDeleted: false,
            Metadata: new Dictionary<string, string>(),
            PermissionMatchMode: permissionMatchMode);
}

