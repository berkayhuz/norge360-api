// <copyright file="SearchDocumentSecurityValidatorTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Search.Application.Security;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class SearchDocumentSecurityValidatorTests
{
    [Fact]
    public void CrmDocument_WithPublicVisibility_ShouldBeInvalid()
    {
        var document = CreateDocument(source: SearchDocumentSource.Crm, visibility: SearchDocumentVisibility.Public);

        var errors = SearchDocumentSecurityValidator.Validate(document);

        errors.Should().ContainSingle(error => error.Contains("cannot use public visibility", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AccountDocument_WithPublicVisibility_ShouldBeInvalid()
    {
        var document = CreateDocument(source: SearchDocumentSource.Account, visibility: SearchDocumentVisibility.Public);

        var errors = SearchDocumentSecurityValidator.Validate(document);

        errors.Should().ContainSingle(error => error.Contains("cannot use public visibility", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PermissionVisibility_WithoutRequiredPermissions_ShouldBeInvalid()
    {
        var document = CreateDocument(
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: []);

        var errors = SearchDocumentSecurityValidator.Validate(document);

        errors.Should().ContainSingle(error => error.Contains("requires at least one required permission", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CrmDynamicEntityDocument_WithoutTenantId_ShouldBeInvalid()
    {
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customer-management.customers.read"],
            tenantId: null,
            type: "customer");

        var errors = SearchDocumentSecurityValidator.Validate(document);

        errors.Should().ContainSingle(error => error.Contains("require TenantId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CrmNavigationDocument_WithoutTenantId_ShouldRemainValid()
    {
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customer-management.customers.read"],
            tenantId: null,
            type: "navigation");

        var errors = SearchDocumentSecurityValidator.Validate(document);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CrmDynamicEntityDocument_WithNeutralLocale_ShouldRemainValid()
    {
        var document = CreateDocument(
            source: SearchDocumentSource.Crm,
            visibility: SearchDocumentVisibility.Permission,
            requiredPermissions: ["crm.customer-management.customers.read"],
            tenantId: Guid.NewGuid(),
            type: "customer",
            locale: SearchDocumentLocales.Neutral);

        var errors = SearchDocumentSecurityValidator.Validate(document);

        errors.Should().BeEmpty();
    }

    private static SearchDocument CreateDocument(
        SearchDocumentSource source = SearchDocumentSource.Public,
        SearchDocumentVisibility visibility = SearchDocumentVisibility.Public,
        IReadOnlyCollection<string>? requiredPermissions = null,
        Guid? tenantId = null,
        SearchPermissionMatchMode permissionMatchMode = SearchPermissionMatchMode.Any,
        string type = "page",
        string locale = "en-US") =>
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
            Locale: locale,
            Tags: [],
            Boost: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            IndexedAtUtc: DateTimeOffset.UtcNow,
            IsDeleted: false,
            Metadata: new Dictionary<string, string>(),
            PermissionMatchMode: permissionMatchMode);
}
