// <copyright file="StaticSearchDocumentRegistryTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.RegularExpressions;
using FluentAssertions;
using Norge360.Search.Application.Security;
using Norge360.Search.Application.StaticDocuments;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.UnitTests;

public sealed class StaticSearchDocumentRegistryTests
{
    [Fact]
    public async Task Registry_ShouldReturnOneDocumentPerSupportedLocaleForEachManifestItem()
    {
        var registry = new StaticSearchDocumentRegistry();

        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().HaveCount(StaticSearchDocumentRegistry.ManifestItems.Count * 2);
        documents.Select(document => document.Locale).Distinct().Should().BeEquivalentTo("en-US", "tr-TR");
    }

    [Fact]
    public async Task Registry_ShouldReturnAtLeastOnePublicDocument()
    {
        var registry = new StaticSearchDocumentRegistry();

        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().Contain(document => document.Visibility == SearchDocumentVisibility.Public);
    }

    [Fact]
    public async Task RegistryDocuments_ShouldPassSecurityValidator()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        var failures = documents
            .Select(document => new
            {
                document.Id,
                Errors = SearchDocumentSecurityValidator.Validate(document)
            })
            .Where(entry => entry.Errors.Count > 0)
            .ToArray();

        failures.Should().BeEmpty();
    }

    [Fact]
    public async Task Registry_ShouldNotContainCrmDocuments()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().NotContain(document => document.Source == SearchDocumentSource.Crm);
    }

    [Fact]
    public async Task Registry_ShouldNotContainAccountPublicDocuments()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().NotContain(document =>
            document.Source == SearchDocumentSource.Account &&
            document.Visibility == SearchDocumentVisibility.Public);
    }

    [Fact]
    public async Task Registry_ShouldNotContainAuthOrAdminPublicDocuments()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().NotContain(document =>
            (document.Source == SearchDocumentSource.Auth || document.Source == SearchDocumentSource.Admin) &&
            document.Visibility == SearchDocumentVisibility.Public);
    }

    [Fact]
    public async Task PermissionVisibilityDocuments_ShouldHaveRequiredPermissions()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        var invalid = documents
            .Where(document => document.Visibility == SearchDocumentVisibility.Permission)
            .Where(document => document.RequiredPermissions.Count == 0)
            .ToArray();

        invalid.Should().BeEmpty();
    }

    [Fact]
    public async Task AnonymousVisibleDocuments_ShouldUseAllowedPublicSources()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        var anonymousVisible = documents
            .Where(document => document.Visibility == SearchDocumentVisibility.Public)
            .ToArray();

        anonymousVisible.Should().OnlyContain(document =>
            !SearchDocumentSecurityValidator.IsPublicSourceBlocked(document.Source));
    }

    [Fact]
    public async Task RegistryUrls_ShouldUseAppLocalPaths_NotLegacySourcePrefixes()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().NotContain(document =>
            document.Url.StartsWith("/crm/", StringComparison.OrdinalIgnoreCase) ||
            document.Url.StartsWith("/account/", StringComparison.OrdinalIgnoreCase) ||
            document.Url.StartsWith("/tools/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Registry_ShouldContainRequiredLocalizedAccountDocuments()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().Contain(d => d.Id == "account-page-mfa-en-US" && d.Title == "MFA" && d.Url == "/security/mfa");
        documents.Should().Contain(d => d.Id == "account-page-mfa-tr-TR" && d.Title == "MFA" && d.Url == "/security/mfa");
    }

    [Fact]
    public async Task Registry_ShouldKeepKeyAppLocalPaths()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().Contain(d => d.Id == "public-page-home-en-US" && d.Url == "/");
        documents.Should().Contain(d => d.Id == "public-page-tools-landing-tr-TR" && d.Url == "/tools");
        documents.Should().Contain(d => d.Id == "account-page-profile-en-US" && d.Url == "/profile");
        documents.Should().Contain(d => d.Id == "account-page-profile-tr-TR" && d.Url == "/profile");
    }

    [Fact]
    public async Task RegistryDocumentIds_ShouldBeMeilisearchSafeAndLocaleSuffixed()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        documents.Should().OnlyContain(document => Regex.IsMatch(document.Id, "^[A-Za-z0-9-]+$"));
        documents.Should().OnlyContain(document =>
            document.Id.EndsWith("-en-US", StringComparison.Ordinal) ||
            document.Id.EndsWith("-tr-TR", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Registry_ShouldIndexLocalizedKeywordsIntoContentWithoutLocalizingTags()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);

        var turkishHome = documents.Single(d => d.Id == "public-page-home-tr-TR");
        turkishHome.Content.Should().NotBeNullOrWhiteSpace();
        turkishHome.Tags.Should().BeEquivalentTo("public", "home", "platform");
    }

    [Fact]
    public void Factory_ShouldFallbackToEnglish_WhenRequestedLocaleKeyIsMissing()
    {
        var localizer = new SearchStaticTextLocalizer(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["en-US"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["title"] = "English title",
                ["summary"] = "English summary"
            },
            ["tr-TR"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["title"] = "Turkce baslik"
            }
        });
        var factory = new StaticSearchDocumentFactory(localizer);
        var item = new StaticSearchManifestItem(
            "test-doc",
            SearchDocumentSource.Public,
            "page",
            "title",
            "summary",
            "/test",
            SearchDocumentVisibility.Public);

        var documents = factory.CreateDocuments(item);

        documents.Single(d => d.Locale == "tr-TR").Summary.Should().Be("English summary");
    }

    [Fact]
    public void Factory_ShouldFail_WhenRequiredKeyIsMissingInRequestedAndFallbackLocale()
    {
        var localizer = new SearchStaticTextLocalizer(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["en-US"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["title"] = "English title"
            },
            ["tr-TR"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["title"] = "Turkce baslik"
            }
        });
        var factory = new StaticSearchDocumentFactory(localizer);
        var item = new StaticSearchManifestItem(
            "test-doc",
            SearchDocumentSource.Public,
            "page",
            "title",
            "missing.summary",
            "/test",
            SearchDocumentVisibility.Public);

        var act = () => factory.CreateDocuments(item);

        act.Should().Throw<KeyNotFoundException>().WithMessage("*missing.summary*");
    }

    [Fact]
    public async Task AuthenticatedVisibility_ShouldRespectAnonymousAccessRules()
    {
        var registry = new StaticSearchDocumentRegistry();
        var documents = await registry.GetDocumentsAsync(CancellationToken.None);
        var anonymous = SearchAccessContext.Anonymous;
        var authenticated = new SearchAccessContext(true, null, null, []);

        var accountProfile = documents.Single(d => d.Id == "account-page-profile-en-US");

        SearchDocumentVisibilityEvaluator.CanAccess(accountProfile, anonymous).Should().BeFalse();
        SearchDocumentVisibilityEvaluator.CanAccess(accountProfile, authenticated).Should().BeTrue();
    }
}

