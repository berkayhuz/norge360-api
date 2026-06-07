// <copyright file="ProfileMutationService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Globalization;
using System.Text.Json;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;
using Norge360.Media.Abstractions;
using Norge360.Search.Contracts.Documents;
using Norge360.Search.Contracts.IntegrationEvents.V1;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileMutationService(
    IUserProfileRepository userProfileRepository,
    IUpdateMyProfileRequestValidator validator,
    IIntegrationEventOutbox integrationEventOutbox,
    IAccountsUnitOfWork unitOfWork,
    IMediaStorageProvider mediaStorageProvider,
    IMediaUrlBuilder mediaUrlBuilder,
    IDiscoveryEventPublisher discoveryEventPublisher) : IProfileMutationService
{
    private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".png",
        ".webp"
    };

    public async Task<UpdateMyProfileResult> UpdateMyProfileAsync(
        Guid authUserId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty)
        {
            return UpdateMyProfileResult.Unauthorized("authenticated_user_required");
        }

        var validationResult = validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return UpdateMyProfileResult.ValidationFailed(
                validationResult.Errors,
                "profile_update_validation_failed");
        }

        var profile = await userProfileRepository.GetTrackedByAuthUserIdAsync(
            authUserId,
            includeDeleted: false,
            cancellationToken);

        if (profile is null)
        {
            return UpdateMyProfileResult.NotFound("profile_not_found");
        }

        var oldVisibility = profile.ProfileVisibility;
        profile.DisplayName = Normalize(request.DisplayName);
        profile.Bio = Normalize(request.Bio);
        profile.Country = Normalize(request.Country);
        profile.City = Normalize(request.City);
        profile.District = Normalize(request.District);
        profile.Occupation = Normalize(request.Occupation);
        profile.Company = Normalize(request.Company);
        profile.Website = Normalize(request.Website);

        var profileVisibility = Normalize(request.ProfileVisibility);
        if (profileVisibility is not null &&
            Enum.TryParse<ProfileVisibility>(profileVisibility, ignoreCase: true, out var parsedVisibility))
        {
            profile.ProfileVisibility = parsedVisibility;
        }

        var commentAudience = Normalize(request.CommentAudience);
        if (commentAudience is not null &&
            Enum.TryParse<PostCommentAudience>(commentAudience, ignoreCase: true, out var parsedAudience))
        {
            profile.CommentAudience = parsedAudience;
        }

        if (request.HideLikeCounts.HasValue)
        {
            profile.HideLikeCounts = request.HideLikeCounts.Value;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await EnqueueSearchSyncEventAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishProfileSnapshotEventAsync(
            oldVisibility == profile.ProfileVisibility ? "ProfileUpdated" : "ProfileVisibilityChanged",
            profile,
            cancellationToken);

        return UpdateMyProfileResult.Success(MapMyProfile(profile));
    }

    public async Task<CompleteAvatarUploadResult> CompleteAvatarUploadAsync(
        Guid authUserId,
        CompleteAvatarUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty)
        {
            return CompleteAvatarUploadResult.Unauthorized("authenticated_user_required");
        }

        var validationErrors = ValidateAvatarStorageKey(authUserId, request.StorageKey, out var normalizedStorageKey);
        if (validationErrors.Count > 0)
        {
            return CompleteAvatarUploadResult.ValidationFailed(
                validationErrors.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase),
                "avatar_complete_validation_failed");
        }

        bool objectExists;
        try
        {
            objectExists = await mediaStorageProvider.ExistsAsync(normalizedStorageKey!, cancellationToken);
        }
        catch
        {
            return CompleteAvatarUploadResult.Failed("avatar_object_exists_check_failed");
        }

        if (!objectExists)
        {
            return CompleteAvatarUploadResult.NotFound("avatar_object_not_found");
        }

        var profile = await userProfileRepository.GetTrackedByAuthUserIdAsync(
            authUserId,
            includeDeleted: false,
            cancellationToken);

        if (profile is null)
        {
            return CompleteAvatarUploadResult.NotFound("profile_not_found");
        }

        var oldAvatarStorageKey = profile.AvatarStorageKey;
        profile.AvatarStorageKey = normalizedStorageKey;
        profile.AvatarUrl = mediaUrlBuilder.BuildPublicUrl(normalizedStorageKey!);
        profile.UpdatedAt = DateTime.UtcNow;

        await EnqueueSearchSyncEventAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishProfileSnapshotEventAsync("ProfileUpdated", profile, cancellationToken);

        await TryDeleteOldAvatarAsync(oldAvatarStorageKey, normalizedStorageKey!, cancellationToken);

        return CompleteAvatarUploadResult.Success(MapMyProfile(profile));
    }

    public async Task<CompleteAvatarUploadResult> CompleteCoverPhotoUploadAsync(
        Guid authUserId,
        CompleteAvatarUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty)
        {
            return CompleteAvatarUploadResult.Unauthorized("authenticated_user_required");
        }

        var validationErrors = ValidateCoverPhotoStorageKey(authUserId, request.StorageKey, out var normalizedStorageKey);
        if (validationErrors.Count > 0)
        {
            return CompleteAvatarUploadResult.ValidationFailed(
                validationErrors.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase),
                "cover_photo_complete_validation_failed");
        }

        bool objectExists;
        try
        {
            objectExists = await mediaStorageProvider.ExistsAsync(normalizedStorageKey!, cancellationToken);
        }
        catch
        {
            return CompleteAvatarUploadResult.Failed("cover_photo_object_exists_check_failed");
        }

        if (!objectExists)
        {
            return CompleteAvatarUploadResult.NotFound("cover_photo_object_not_found");
        }

        var profile = await userProfileRepository.GetTrackedByAuthUserIdAsync(
            authUserId,
            includeDeleted: false,
            cancellationToken);

        if (profile is null)
        {
            return CompleteAvatarUploadResult.NotFound("profile_not_found");
        }

        var oldCoverPhotoStorageKey = profile.CoverPhotoStorageKey;
        profile.CoverPhotoStorageKey = normalizedStorageKey;
        profile.CoverPhotoUrl = mediaUrlBuilder.BuildPublicUrl(normalizedStorageKey!);
        profile.UpdatedAt = DateTime.UtcNow;

        await EnqueueSearchSyncEventAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishProfileSnapshotEventAsync("ProfileUpdated", profile, cancellationToken);

        await TryDeleteOldCoverPhotoAsync(oldCoverPhotoStorageKey, normalizedStorageKey!, cancellationToken);

        return CompleteAvatarUploadResult.Success(MapMyProfile(profile));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnqueueSearchSyncEventAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;

        if (profile.ProfileVisibility == ProfileVisibility.Hidden || !profile.IsActive)
        {
            var deleteEvent = new SearchDocumentDeleteRequestedV1(
                EventId: eventId,
                DocumentId: SearchUserDocumentFactory.BuildDocumentId(profile),
                Source: SearchDocumentSource.Forum,
                Type: "user",
                TenantId: null,
                CorrelationId: null,
                CausationId: profile.AuthUserId.ToString("D"),
                OccurredAtUtc: occurredAtUtc);

            await integrationEventOutbox.AddAsync(
                eventId,
                SearchDocumentDeleteRequestedV1.EventName,
                SearchDocumentDeleteRequestedV1.EventVersion,
                SearchDocumentDeleteRequestedV1.RoutingKey,
                "Norge360.Accounts",
                deleteEvent,
                correlationId: null,
                traceId: null,
                occurredAtUtc,
                cancellationToken);

            return;
        }

        var indexEvent = new SearchDocumentIndexRequestedV1(
            EventId: eventId,
            Document: SearchUserDocumentFactory.Build(profile, DateTimeOffset.UtcNow),
            CorrelationId: null,
            CausationId: profile.AuthUserId.ToString("D"),
            OccurredAtUtc: occurredAtUtc);

        await integrationEventOutbox.AddAsync(
            eventId,
            SearchDocumentIndexRequestedV1.EventName,
            SearchDocumentIndexRequestedV1.EventVersion,
            SearchDocumentIndexRequestedV1.RoutingKey,
            "Norge360.Accounts",
            indexEvent,
            correlationId: null,
            traceId: null,
            occurredAtUtc,
            cancellationToken);
    }

    private async Task TryDeleteOldAvatarAsync(
        string? oldAvatarStorageKey,
        string newAvatarStorageKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarStorageKey))
        {
            return;
        }

        if (string.Equals(oldAvatarStorageKey, newAvatarStorageKey, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await mediaStorageProvider.DeleteAsync(oldAvatarStorageKey, cancellationToken);
        }
        catch
        {
            // Best-effort cleanup: old avatar delete failures should not fail profile update.
        }
    }

    private async Task TryDeleteOldCoverPhotoAsync(
        string? oldCoverPhotoStorageKey,
        string newCoverPhotoStorageKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldCoverPhotoStorageKey))
        {
            return;
        }

        if (string.Equals(oldCoverPhotoStorageKey, newCoverPhotoStorageKey, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await mediaStorageProvider.DeleteAsync(oldCoverPhotoStorageKey, cancellationToken);
        }
        catch
        {
            // Best-effort cleanup: old cover photo delete failures should not fail profile update.
        }
    }

    private async Task PublishProfileSnapshotEventAsync(string eventType, UserProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            await discoveryEventPublisher.PublishAsync(
                new DiscoveryEventEnvelope(
                    eventType,
                    "Accounts",
                    "UserProfile",
                    profile.Id.ToString("D"),
                    profile.AuthUserId,
                    profile.Id,
                    profile.AuthUserId,
                    profile.Id,
                    "UserProfile",
                    profile.Id.ToString("D"),
                    $"accounts:profile-snapshot:{profile.Id:D}:{eventType}:{profile.UpdatedAt?.Ticks ?? profile.CreatedAt.Ticks}",
                    DateTime.UtcNow,
                    BuildSnapshotMetadata(profile)),
                cancellationToken);
        }
        catch
        {
            // Discovery snapshots are eventually consistent and should not block profile updates.
        }
    }

    private static string BuildSnapshotMetadata(UserProfile profile) =>
        JsonSerializer.Serialize(new
        {
            username = profile.Username,
            displayName = profile.DisplayName,
            avatarUrl = profile.AvatarUrl,
            bio = profile.Bio,
            followersCount = profile.FollowersCount,
            postsCount = profile.PostsCount,
            visibility = profile.ProfileVisibility.ToString(),
            isVerified = profile.IsVerified,
            isActive = profile.IsActive,
            isDeleted = profile.IsDeleted
        });

    private static Dictionary<string, List<string>> ValidateAvatarStorageKey(
        Guid authUserId,
        string? storageKey,
        out string? normalizedStorageKey)
    {
        normalizedStorageKey = null;
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var key = storageKey?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            AddError(errors, "storageKey", "storage_key_required");
            return errors;
        }

        if (Uri.TryCreate(key, UriKind.Absolute, out _))
        {
            AddError(errors, "storageKey", "storage_key_must_be_relative");
            return errors;
        }

        if (key.Contains('\\', StringComparison.Ordinal) ||
            key.Contains("//", StringComparison.Ordinal) ||
            key.Contains("..", StringComparison.Ordinal) ||
            key.Contains('?', StringComparison.Ordinal) ||
            key.Contains('#', StringComparison.Ordinal))
        {
            AddError(errors, "storageKey", "storage_key_invalid_path");
            return errors;
        }

        var expectedPrefix = string.Create(
            CultureInfo.InvariantCulture,
            $"profiles/{authUserId:D}/avatar/");

        if (!key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "storageKey", "storage_key_ownership_mismatch");
            return errors;
        }

        var extension = Path.GetExtension(key);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedAvatarExtensions.Contains(extension))
        {
            AddError(errors, "storageKey", "storage_key_extension_not_allowed");
            return errors;
        }

        normalizedStorageKey = key;
        return errors;
    }

    private static Dictionary<string, List<string>> ValidateCoverPhotoStorageKey(
        Guid authUserId,
        string? storageKey,
        out string? normalizedStorageKey)
    {
        normalizedStorageKey = null;
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var key = storageKey?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            AddError(errors, "storageKey", "storage_key_required");
            return errors;
        }

        if (Uri.TryCreate(key, UriKind.Absolute, out _))
        {
            AddError(errors, "storageKey", "storage_key_must_be_relative");
            return errors;
        }

        if (key.Contains('\\', StringComparison.Ordinal) ||
            key.Contains("//", StringComparison.Ordinal) ||
            key.Contains("..", StringComparison.Ordinal) ||
            key.Contains('?', StringComparison.Ordinal) ||
            key.Contains('#', StringComparison.Ordinal))
        {
            AddError(errors, "storageKey", "storage_key_invalid_path");
            return errors;
        }

        var expectedPrefix = string.Create(
            CultureInfo.InvariantCulture,
            $"profiles/{authUserId:D}/cover-photo/");

        if (!key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "storageKey", "storage_key_ownership_mismatch");
            return errors;
        }

        var extension = Path.GetExtension(key);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedAvatarExtensions.Contains(extension))
        {
            AddError(errors, "storageKey", "storage_key_extension_not_allowed");
            return errors;
        }

        normalizedStorageKey = key;
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

    private static MyProfileResponse MapMyProfile(UserProfile profile) => new(
        profile.Id,
        profile.AuthUserId,
        profile.Username,
        profile.NormalizedUsername,
        profile.DisplayName,
        profile.Bio,
        profile.AvatarUrl,
        profile.CoverPhotoUrl,
        profile.Country,
        profile.City,
        profile.District,
        profile.Occupation,
        profile.Company,
        profile.Website,
        profile.FollowersCount,
        profile.FollowingCount,
        profile.PostsCount,
        profile.IsVerified,
        profile.AccountType.ToString(),
        profile.ProfileVisibility.ToString(),
        profile.CommentAudience.ToString(),
        profile.HideLikeCounts,
        profile.LastSeenAt,
        profile.CreatedAt,
        profile.UpdatedAt);
}
