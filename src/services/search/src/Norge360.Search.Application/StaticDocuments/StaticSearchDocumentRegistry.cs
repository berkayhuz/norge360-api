// <copyright file="StaticSearchDocumentRegistry.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.StaticDocuments;

public sealed class StaticSearchDocumentRegistry : IStaticSearchDocumentRegistry
{
    public StaticSearchDocumentRegistry()
        : this(new StaticSearchDocumentFactory(SearchStaticTextLocalizer.CreateDefault()))
    {
    }

    public StaticSearchDocumentRegistry(StaticSearchDocumentFactory factory)
    {
        Factory = factory;
    }

    public static IReadOnlyCollection<StaticSearchManifestItem> ManifestItems { get; } =
    [
        // Public website pages
        CreateItem("public-page-home", SearchDocumentSource.Public, "page", "public.search.home.title", "public.search.home.summary", "/", SearchDocumentVisibility.Public, tags: ["public", "home", "platform"], boost: 2.0, keywordKeys: ["public.search.home.keywords"]),
        CreateItem("public-page-pricing", SearchDocumentSource.Public, "page", "public.search.pricing.title", "public.search.pricing.summary", "/pricing", SearchDocumentVisibility.Public, tags: ["public", "pricing", "plans"], boost: 1.8, keywordKeys: ["public.search.pricing.keywords"]),
        CreateItem("public-page-product", SearchDocumentSource.Public, "page", "public.search.product.title", "public.search.product.summary", "/product", SearchDocumentVisibility.Public, tags: ["public", "product", "features"], boost: 1.6, keywordKeys: ["public.search.product.keywords"]),
        CreateItem("public-page-tools-landing", SearchDocumentSource.Public, "page", "public.search.tools.title", "public.search.tools.summary", "/tools", SearchDocumentVisibility.Public, tags: ["public", "tools", "utilities"], boost: 1.7, keywordKeys: ["public.search.tools.keywords"]),

        // Public tools
        CreateItem("tools-tool-qr-generator", SearchDocumentSource.Tools, "tool", "tools.search.qrGenerator.title", "tools.search.qrGenerator.summary", "/qr-generator", SearchDocumentVisibility.Public, tags: ["tools", "qr", "generator"], boost: 1.5, keywordKeys: ["tools.search.qrGenerator.keywords"]),
        CreateItem("tools-tool-png-to-jpg", SearchDocumentSource.Tools, "tool", "tools.search.pngToJpg.title", "tools.search.pngToJpg.summary", "/image/png-to-jpg", SearchDocumentVisibility.Public, tags: ["tools", "image", "conversion"], boost: 1.4, keywordKeys: ["tools.search.pngToJpg.keywords"]),

        // Account pages (authenticated only)
        CreateItem("account-page-profile", SearchDocumentSource.Account, "page", "account.profile.title", "account.profile.searchSummary", "/profile", SearchDocumentVisibility.Authenticated, tags: ["account", "profile", "settings"], boost: 1.5, keywordKeys: ["account.profile.searchKeywords"]),
        CreateItem("account-page-security", SearchDocumentSource.Account, "page", "account.security.title", "account.security.searchSummary", "/security", SearchDocumentVisibility.Authenticated, tags: ["account", "security", "mfa"], boost: 1.6, keywordKeys: ["account.security.searchKeywords"]),
        CreateItem("account-page-preferences", SearchDocumentSource.Account, "page", "account.preferences.title", "account.preferences.searchSummary", "/preferences", SearchDocumentVisibility.Authenticated, tags: ["account", "preferences", "settings"], boost: 1.4, keywordKeys: ["account.preferences.searchKeywords"]),
        CreateItem("account-page-workspaces", SearchDocumentSource.Account, "page", "account.workspaces.title", "account.workspaces.searchSummary", "/workspaces", SearchDocumentVisibility.Authenticated, tags: ["account", "workspaces", "organizations"], boost: 1.3, keywordKeys: ["account.workspaces.searchKeywords"]),
        CreateItem("account-page-sessions", SearchDocumentSource.Account, "page", "account.sessions.title", "account.sessions.searchSummary", "/security/sessions", SearchDocumentVisibility.Authenticated, tags: ["account", "sessions", "security"], boost: 1.5, keywordKeys: ["account.sessions.searchKeywords"]),
        CreateItem("account-page-mfa", SearchDocumentSource.Account, "page", "account.mfa.title", "account.mfa.searchSummary", "/security/mfa", SearchDocumentVisibility.Authenticated, tags: ["account", "mfa", "security"], boost: 1.5, keywordKeys: ["account.mfa.searchKeywords"]),
        CreateItem("account-page-password", SearchDocumentSource.Account, "page", "account.security.password.title", "account.security.password.searchSummary", "/security/password", SearchDocumentVisibility.Authenticated, tags: ["account", "password", "security"], boost: 1.4, keywordKeys: ["account.security.password.searchKeywords"]),
        CreateItem("account-page-notifications", SearchDocumentSource.Account, "page", "account.notifications.title", "account.notifications.searchSummary", "/notifications", SearchDocumentVisibility.Authenticated, tags: ["account", "notifications", "preferences"], boost: 1.2, keywordKeys: ["account.notifications.searchKeywords"]),
        CreateItem("account-page-audit", SearchDocumentSource.Account, "page", "account.audit.title", "account.audit.searchSummary", "/audit", SearchDocumentVisibility.Authenticated, tags: ["account", "audit", "activity"], boost: 1.2, keywordKeys: ["account.audit.searchKeywords"]),
        CreateItem("account-page-team", SearchDocumentSource.Account, "page", "account.team.title", "account.team.searchSummary", "/settings/team", SearchDocumentVisibility.Authenticated, tags: ["account", "team", "settings"], boost: 1.2, keywordKeys: ["account.team.searchKeywords"]),
        CreateItem("account-page-privacy", SearchDocumentSource.Account, "page", "account.privacy.title", "account.privacy.searchSummary", "/privacy", SearchDocumentVisibility.Authenticated, tags: ["account", "privacy", "consent"], boost: 1.2, keywordKeys: ["account.privacy.searchKeywords"]),

    ];

    private StaticSearchDocumentFactory Factory { get; }

    public Task<IReadOnlyCollection<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Factory.CreateDocuments(ManifestItems));
    }

    private static StaticSearchManifestItem CreateItem(
        string id,
        SearchDocumentSource source,
        string type,
        string titleKey,
        string summaryKey,
        string url,
        SearchDocumentVisibility visibility,
        IReadOnlyCollection<string>? requiredPermissions = null,
        IReadOnlyCollection<string>? tags = null,
        double boost = 1,
        string? contentKey = null,
        IReadOnlyCollection<string>? keywordKeys = null,
        SearchPermissionMatchMode permissionMatchMode = SearchPermissionMatchMode.Any,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(id, source, type, titleKey, summaryKey, url, visibility)
        {
            RequiredPermissions = requiredPermissions ?? [],
            Tags = tags ?? [],
            Boost = boost,
            ContentKey = contentKey,
            KeywordKeys = keywordKeys ?? [],
            PermissionMatchMode = permissionMatchMode,
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };
}
