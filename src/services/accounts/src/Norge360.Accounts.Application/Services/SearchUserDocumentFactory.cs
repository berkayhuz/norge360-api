// <copyright file="SearchUserDocumentFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;
using Norge360.Search.Contracts.Documents;

namespace Norge360.Accounts.Application.Services;

internal static class SearchUserDocumentFactory
{
    public static string BuildDocumentId(UserProfile profile) => $"user-{profile.Id:D}";

    public static SearchDocument Build(UserProfile profile, DateTimeOffset nowUtc)
    {
        var displayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Username : profile.DisplayName.Trim();
        var bio = profile.Bio?.Trim() ?? string.Empty;
        var username = profile.Username.Trim();
        var normalizedUsername = profile.NormalizedUsername.Trim();
        var content = string.Join('\n', new[] { username, normalizedUsername, displayName, bio }.Where(x => !string.IsNullOrWhiteSpace(x)));

        return new SearchDocument(
            Id: BuildDocumentId(profile),
            Source: SearchDocumentSource.Forum,
            Type: "user",
            Title: displayName,
            Summary: bio,
            Content: content,
            Url: $"/{username}",
            TenantId: null,
            RequiredPermissions: [],
            Visibility: ResolveVisibility(profile.ProfileVisibility),
            Locale: "en-US",
            Tags: BuildTags(profile),
            Boost: profile.IsVerified ? 1.15 : 1.0,
            CreatedAtUtc: ToUtcOffset(profile.CreatedAt),
            UpdatedAtUtc: ToUtcOffset(profile.UpdatedAt ?? profile.CreatedAt),
            IndexedAtUtc: nowUtc,
            IsDeleted: profile.IsDeleted || !profile.IsActive || profile.ProfileVisibility == ProfileVisibility.Hidden,
            Metadata: BuildMetadata(profile),
            ExternalId: profile.AuthUserId.ToString("D"),
            OwnerUserId: profile.AuthUserId,
            AssignedUserIds: [],
            SourceVersion: ToUtcOffset(profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeMilliseconds().ToString(),
            SourceUpdatedAtUtc: ToUtcOffset(profile.UpdatedAt ?? profile.CreatedAt),
            PermissionMatchMode: SearchPermissionMatchMode.Any);
    }

    private static SearchDocumentVisibility ResolveVisibility(ProfileVisibility visibility) =>
        visibility switch
        {
            ProfileVisibility.Public => SearchDocumentVisibility.Public,
            ProfileVisibility.Private => SearchDocumentVisibility.Authenticated,
            ProfileVisibility.FollowersOnly => SearchDocumentVisibility.Authenticated,
            ProfileVisibility.Hidden => SearchDocumentVisibility.Authenticated,
            _ => SearchDocumentVisibility.Authenticated
        };

    private static IReadOnlyCollection<string> BuildTags(UserProfile profile)
    {
        var tags = new List<string> { "user", "profile" };
        if (profile.IsVerified)
        {
            tags.Add("verified");
        }

        return tags;
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(UserProfile profile) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["profileId"] = profile.Id.ToString("D"),
            ["username"] = profile.Username,
            ["normalizedUsername"] = profile.NormalizedUsername,
            ["displayName"] = profile.DisplayName ?? string.Empty,
            ["bio"] = profile.Bio ?? string.Empty,
            ["avatarUrl"] = profile.AvatarUrl ?? string.Empty,
            ["isVerified"] = profile.IsVerified ? "true" : "false",
            ["followersCount"] = profile.FollowersCount.ToString(),
            ["followingCount"] = profile.FollowingCount.ToString(),
            ["profileVisibility"] = profile.ProfileVisibility.ToString()
        };

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return new DateTimeOffset(utc);
    }
}
