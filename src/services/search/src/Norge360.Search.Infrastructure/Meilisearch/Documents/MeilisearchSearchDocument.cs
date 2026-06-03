// <copyright file="MeilisearchSearchDocument.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json.Serialization;

namespace Norge360.Search.Infrastructure.Meilisearch.Documents;

internal sealed record MeilisearchSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("tenantId")]
    public Guid? TenantId { get; init; }

    [JsonPropertyName("requiredPermissions")]
    public IReadOnlyCollection<string> RequiredPermissions { get; init; } = [];

    [JsonPropertyName("visibility")]
    public string Visibility { get; init; } = string.Empty;

    [JsonPropertyName("permissionMatchMode")]
    public string PermissionMatchMode { get; init; } = string.Empty;

    [JsonPropertyName("locale")]
    public string Locale { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<string> Tags { get; init; } = [];

    [JsonPropertyName("boost")]
    public double Boost { get; init; }

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; init; }

    [JsonPropertyName("indexedAtUtc")]
    public DateTimeOffset IndexedAtUtc { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("ownerUserId")]
    public Guid? OwnerUserId { get; init; }

    [JsonPropertyName("assignedUserIds")]
    public IReadOnlyCollection<Guid> AssignedUserIds { get; init; } = [];

    [JsonPropertyName("sourceVersion")]
    public string? SourceVersion { get; init; }

    [JsonPropertyName("sourceUpdatedAtUtc")]
    public DateTimeOffset? SourceUpdatedAtUtc { get; init; }

    [JsonPropertyName("_rankingScore")]
    public double? RankingScore { get; init; }
}

