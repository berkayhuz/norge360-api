// <copyright file="UpdateMyProfileRequestValidator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Domain.Enums;

namespace Norge360.Accounts.Application.Services;

public sealed class UpdateMyProfileRequestValidator : IUpdateMyProfileRequestValidator
{
    public UpdateMyProfileRequestValidationResult Validate(UpdateMyProfileRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        ValidateLength("displayName", request.DisplayName, 100, errors);
        ValidateLength("bio", request.Bio, 500, errors);
        ValidateLength("country", request.Country, 100, errors);
        ValidateLength("city", request.City, 100, errors);
        ValidateLength("district", request.District, 100, errors);
        ValidateLength("occupation", request.Occupation, 100, errors);
        ValidateLength("company", request.Company, 100, errors);
        ValidateWebsite(request.Website, errors);
        ValidateProfileVisibility(request.ProfileVisibility, errors);
        ValidateCommentAudience(request.CommentAudience, errors);

        if (errors.Count == 0)
        {
            return UpdateMyProfileRequestValidationResult.Success();
        }

        var normalized = errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        return UpdateMyProfileRequestValidationResult.Failure(normalized);
    }

    private static void ValidateLength(
        string fieldName,
        string? value,
        int maxLength,
        IDictionary<string, List<string>> errors)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return;
        }

        if (normalized.Length <= maxLength)
        {
            return;
        }

        AddError(errors, fieldName, $"{fieldName}_length_invalid");
    }

    private static void ValidateWebsite(
        string? value,
        IDictionary<string, List<string>> errors)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return;
        }

        if (normalized.Length > 256)
        {
            AddError(errors, "website", "website_length_invalid");
            return;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            AddError(errors, "website", "website_invalid");
            return;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "website", "website_scheme_invalid");
        }
    }

    private static void ValidateProfileVisibility(
        string? value,
        IDictionary<string, List<string>> errors)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return;
        }

        if (!Enum.TryParse<ProfileVisibility>(normalized, ignoreCase: true, out _))
        {
            AddError(errors, "profileVisibility", "profile_visibility_invalid");
            return;
        }

        if (!string.Equals(normalized, nameof(ProfileVisibility.Public), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, nameof(ProfileVisibility.Private), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, nameof(ProfileVisibility.FollowersOnly), StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "profileVisibility", "profile_visibility_invalid");
        }
    }

    private static void ValidateCommentAudience(
        string? value,
        IDictionary<string, List<string>> errors)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return;
        }

        if (!Enum.TryParse<PostCommentAudience>(normalized, ignoreCase: true, out _))
        {
            AddError(errors, "commentAudience", "comment_audience_invalid");
            return;
        }

        if (!string.Equals(normalized, nameof(PostCommentAudience.Followers), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, nameof(PostCommentAudience.MutualFollowers), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, nameof(PostCommentAudience.Closed), StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "commentAudience", "comment_audience_invalid");
        }
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string reason)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }

        list.Add(reason);
    }
}
