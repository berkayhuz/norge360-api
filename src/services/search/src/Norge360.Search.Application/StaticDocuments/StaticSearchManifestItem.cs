// <copyright file="StaticSearchManifestItem.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Contracts.Documents;

namespace Norge360.Search.Application.StaticDocuments;

public sealed record StaticSearchManifestItem(
    string Id,
    SearchDocumentSource Source,
    string Type,
    string TitleKey,
    string SummaryKey,
    string Url,
    SearchDocumentVisibility Visibility)
{
    public string? ContentKey { get; init; }
    public IReadOnlyCollection<string> KeywordKeys { get; init; } = [];
    public Guid? TenantId { get; init; }
    public IReadOnlyCollection<string> RequiredPermissions { get; init; } = [];
    public SearchPermissionMatchMode PermissionMatchMode { get; init; } = SearchPermissionMatchMode.Any;
    public IReadOnlyCollection<string> Tags { get; init; } = [];
    public double Boost { get; init; } = 1;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
