// <copyright file="StaticSearchDocumentFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Application.Localization;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.StaticDocuments;

public sealed class StaticSearchDocumentFactory(ISearchStaticTextLocalizer localizer)
{
    private static readonly DateTimeOffset RegistryTimestampUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public IReadOnlyCollection<SearchDocument> CreateDocuments(IReadOnlyCollection<StaticSearchManifestItem> manifestItems)
    {
        if (manifestItems.Count == 0)
        {
            return [];
        }

        return manifestItems
            .SelectMany(CreateDocuments)
            .ToArray();
    }

    public IReadOnlyCollection<SearchDocument> CreateDocuments(StaticSearchManifestItem item) =>
        localizer.SupportedLocales
            .Select(locale => CreateDocument(item, SearchLocale.CanonicalizeStaticLocale(locale)))
            .ToArray();

    private SearchDocument CreateDocument(StaticSearchManifestItem item, string locale)
    {
        var title = localizer.ResolveRequired(item.TitleKey, locale);
        var summary = localizer.ResolveRequired(item.SummaryKey, locale);
        var keywords = localizer.ResolveKeywords(item.KeywordKeys, locale);
        var content = BuildContent(title, summary, localizer.ResolveOptional(item.ContentKey, locale), keywords);

        return new SearchDocument(
            Id: BuildLocalizedDocumentId(item.Id, locale),
            Source: item.Source,
            Type: item.Type,
            Title: title,
            Summary: summary,
            Content: content,
            Url: item.Url,
            TenantId: item.TenantId,
            RequiredPermissions: NormalizeStringList(item.RequiredPermissions),
            Visibility: item.Visibility,
            Locale: locale,
            Tags: NormalizeStringList(item.Tags),
            Boost: item.Boost,
            CreatedAtUtc: RegistryTimestampUtc,
            UpdatedAtUtc: RegistryTimestampUtc,
            IndexedAtUtc: DateTimeOffset.MinValue,
            IsDeleted: false,
            Metadata: NormalizeMetadata(item.Metadata),
            PermissionMatchMode: item.PermissionMatchMode);
    }

    public static string BuildLocalizedDocumentId(string id, string locale) =>
        $"{id}-{SearchLocale.CanonicalizeStaticLocale(locale)}";

    private static string BuildContent(
        string title,
        string summary,
        string? content,
        IReadOnlyCollection<string> keywords)
    {
        var parts = new List<string>
        {
            title,
            summary
        };

        if (!string.IsNullOrWhiteSpace(content))
        {
            parts.Add(content.Trim());
        }

        parts.AddRange(keywords);

        return string.Join('\n', parts);
    }

    private static IReadOnlyCollection<string> NormalizeStringList(IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return metadata
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .ToDictionary(
                kvp => kvp.Key.Trim(),
                kvp => kvp.Value ?? string.Empty,
                StringComparer.Ordinal);
    }
}
