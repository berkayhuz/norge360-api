// <copyright file="ProfileAvatarUploadIntentService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;
using Norge360.Media.Abstractions;
using Norge360.Media.Models;
using Norge360.Media.Options;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileAvatarUploadIntentService(
    IMediaUploadUrlSigner uploadUrlSigner,
    IMediaUrlBuilder mediaUrlBuilder,
    IOptions<MediaOptions> mediaOptions) : IProfileAvatarUploadIntentService
{
    private const long MaxAvatarBytes = 5L * 1024 * 1024;
    private static readonly TimeSpan IntentTtl = TimeSpan.FromMinutes(5);

    private static readonly IReadOnlyDictionary<string, string> ContentTypeToExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    public Task<CreateAvatarUploadIntentResult> CreateAsync(
        Guid authUserId,
        CreateAvatarUploadIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        if (authUserId == Guid.Empty)
        {
            return Task.FromResult(CreateAvatarUploadIntentResult.Unauthorized("authenticated_user_required"));
        }

        var errors = Validate(request, mediaOptions.Value);
        if (errors.Count > 0)
        {
            return Task.FromResult(
                CreateAvatarUploadIntentResult.ValidationFailed(
                    errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase),
                    "avatar_upload_intent_validation_failed"));
        }

        var normalizedContentType = request.ContentType.Trim().ToLowerInvariant();
        var extension = ContentTypeToExtension[normalizedContentType];
        var now = DateTimeOffset.UtcNow;
        var storageKey =
            $"profiles/{authUserId.ToString("D").ToLowerInvariant()}/avatar/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var expiresAt = now.Add(IntentTtl);

        try
        {
            var presigned = uploadUrlSigner.CreatePresignedUploadUrl(
                new MediaUploadUrlRequest(
                    storageKey,
                    normalizedContentType,
                    "PUT",
                    expiresAt));

            var response = new AvatarUploadIntentResponse(
                presigned.UploadUrl,
                "PUT",
                storageKey,
                mediaUrlBuilder.BuildPublicUrl(storageKey),
                presigned.ExpiresAt,
                presigned.Headers);

            return Task.FromResult(CreateAvatarUploadIntentResult.Success(response));
        }
        catch (NotSupportedException)
        {
            return Task.FromResult(CreateAvatarUploadIntentResult.Failed("avatar_upload_intent_not_supported"));
        }
        catch
        {
            return Task.FromResult(CreateAvatarUploadIntentResult.Failed("avatar_upload_intent_failed"));
        }
    }

    private static Dictionary<string, List<string>> Validate(CreateAvatarUploadIntentRequest request, MediaOptions options)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var contentType = request.ContentType?.Trim();
        if (string.IsNullOrWhiteSpace(contentType))
        {
            AddError(errors, "contentType", "content_type_required");
        }
        else if (!ContentTypeToExtension.ContainsKey(contentType))
        {
            AddError(errors, "contentType", "content_type_not_allowed");
        }
        else if (!options.AllowedImageContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            AddError(errors, "contentType", "content_type_not_allowed");
        }

        if (request.ContentLength <= 0)
        {
            AddError(errors, "contentLength", "content_length_invalid");
        }
        else
        {
            if (request.ContentLength > MaxAvatarBytes)
            {
                AddError(errors, "contentLength", "content_length_exceeded");
            }

            if (request.ContentLength > options.MaxImageBytes)
            {
                AddError(errors, "contentLength", "content_length_exceeded");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.FileName) && !string.IsNullOrWhiteSpace(contentType))
        {
            var extension = Path.GetExtension(request.FileName.Trim());
            if (!string.IsNullOrWhiteSpace(extension))
            {
                if (!options.AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    AddError(errors, "fileName", "file_extension_not_allowed");
                }
                else if (ContentTypeToExtension.TryGetValue(contentType, out var mappedExtension) &&
                         !string.Equals(extension, mappedExtension, StringComparison.OrdinalIgnoreCase) &&
                         !(string.Equals(mappedExtension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)))
                {
                    AddError(errors, "fileName", "file_extension_content_type_mismatch");
                }
            }
        }

        return errors;
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string error)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(error);
    }
}

