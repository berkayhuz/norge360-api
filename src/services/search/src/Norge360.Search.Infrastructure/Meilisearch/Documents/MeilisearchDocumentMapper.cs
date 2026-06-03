// <copyright file="MeilisearchDocumentMapper.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Application.Security;
using Norge360.Search.Application.Queries;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Infrastructure.Meilisearch.Documents;

internal sealed class MeilisearchDocumentMapper
{
    public MeilisearchSearchDocument ToStoredDocument(SearchDocument document)
    {
        var validationErrors = SearchDocumentSecurityValidator.Validate(document);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot map invalid search document '{document.Id}': {string.Join("; ", validationErrors)}");
        }

        return new MeilisearchSearchDocument
        {
            Id = document.Id,
            Source = document.Source.ToString(),
            Type = document.Type,
            Title = document.Title,
            Summary = document.Summary,
            Content = document.Content,
            Url = document.Url,
            TenantId = document.TenantId,
            RequiredPermissions = NormalizeStringList(document.RequiredPermissions),
            Visibility = document.Visibility.ToString(),
            PermissionMatchMode = document.PermissionMatchMode.ToString(),
            Locale = document.Locale,
            Tags = NormalizeStringList(document.Tags),
            Boost = document.Boost,
            CreatedAtUtc = document.CreatedAtUtc,
            UpdatedAtUtc = document.UpdatedAtUtc,
            IndexedAtUtc = document.IndexedAtUtc,
            IsDeleted = document.IsDeleted,
            Metadata = NormalizeMetadata(document.Metadata),
            ExternalId = document.ExternalId,
            OwnerUserId = document.OwnerUserId,
            AssignedUserIds = NormalizeGuidList(document.AssignedUserIds),
            SourceVersion = document.SourceVersion,
            SourceUpdatedAtUtc = document.SourceUpdatedAtUtc
        };
    }

    public SearchDocument ToSearchDocument(MeilisearchSearchDocument document)
    {
        var source = ParseEnum<SearchDocumentSource>(document.Source, nameof(document.Source));
        var visibility = ParseEnum<SearchDocumentVisibility>(document.Visibility, nameof(document.Visibility));
        var permissionMatchMode = ParseEnum<SearchPermissionMatchMode>(document.PermissionMatchMode, nameof(document.PermissionMatchMode));

        return new SearchDocument(
            Id: document.Id,
            Source: source,
            Type: document.Type,
            Title: document.Title,
            Summary: document.Summary,
            Content: document.Content,
            Url: document.Url,
            TenantId: document.TenantId,
            RequiredPermissions: NormalizeStringList(document.RequiredPermissions),
            Visibility: visibility,
            Locale: document.Locale,
            Tags: NormalizeStringList(document.Tags),
            Boost: document.Boost,
            CreatedAtUtc: document.CreatedAtUtc,
            UpdatedAtUtc: document.UpdatedAtUtc,
            IndexedAtUtc: document.IndexedAtUtc,
            IsDeleted: document.IsDeleted,
            Metadata: NormalizeMetadata(document.Metadata),
            ExternalId: document.ExternalId,
            OwnerUserId: document.OwnerUserId,
            AssignedUserIds: NormalizeGuidList(document.AssignedUserIds),
            SourceVersion: document.SourceVersion,
            SourceUpdatedAtUtc: document.SourceUpdatedAtUtc,
            PermissionMatchMode: permissionMatchMode);
    }

    public SearchResultItem ToSearchResultItem(MeilisearchSearchDocument document)
    {
        var source = ParseEnum<SearchDocumentSource>(document.Source, nameof(document.Source));
        var visibility = ParseEnum<SearchDocumentVisibility>(document.Visibility, nameof(document.Visibility));
        var username = ReadMetadata(document.Metadata, "username", "normalizedUsername");
        var displayName = ReadMetadata(document.Metadata, "displayName");
        var avatarUrl = ReadMetadata(document.Metadata, "avatarUrl");
        var bio = ReadMetadata(document.Metadata, "bio");
        var isVerified = TryParseBool(ReadMetadata(document.Metadata, "isVerified"));
        var followersCount = TryParseInt(ReadMetadata(document.Metadata, "followersCount"));

        return new SearchResultItem(
            Id: document.Id,
            Source: source,
            Type: document.Type,
            Title: document.Title,
            Summary: document.Summary,
            Url: document.Url,
            Visibility: visibility,
            Locale: document.Locale,
            Tags: NormalizeStringList(document.Tags),
            Username: string.IsNullOrWhiteSpace(username) ? null : username,
            DisplayName: string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            AvatarUrl: string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl,
            Bio: string.IsNullOrWhiteSpace(bio) ? null : bio,
            FollowersCount: followersCount,
            IsVerified: isVerified,
            RankingScore: document.RankingScore);
    }

    private static TEnum ParseEnum<TEnum>(string rawValue, string fieldName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(rawValue, true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid {fieldName} value '{rawValue}'.");
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

    private static IReadOnlyCollection<Guid> NormalizeGuidList(IReadOnlyCollection<Guid>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values.Distinct().ToArray();
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
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadMetadata(IReadOnlyDictionary<string, string> metadata, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static bool? TryParseBool(string raw) =>
        bool.TryParse(raw, out var parsed) ? parsed : null;

    private static int? TryParseInt(string raw) =>
        int.TryParse(raw, out var parsed) ? parsed : null;
}
