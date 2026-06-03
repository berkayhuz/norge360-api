// <copyright file="ProfileProvisioningService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.IntegrationEvents.V1;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;
using Norge360.Clock;
using Norge360.Search.Contracts.IntegrationEvents.V1;
using System.Text.Json;

namespace Norge360.Accounts.Application.Services;

public sealed class ProfileProvisioningService(
    IUserProfileRepository userProfileRepository,
    IUsernameAvailabilityService usernameAvailabilityService,
    IUsernameNormalizer usernameNormalizer,
    IIntegrationEventOutbox integrationEventOutbox,
    IAccountsUnitOfWork unitOfWork,
    IDiscoveryEventPublisher discoveryEventPublisher,
    IClock clock) : IProfileProvisioningService
{
    private const int MaximumDisplayNameLength = 100;
    private static readonly int[] FallbackIdentifierLengths = [12, 16, 24];

    public async Task<UserProfile> ProvisionAsync(
        UserRegisteredV1 message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserRegisteredV1 must contain a non-empty user identifier.", nameof(message));
        }

        var existingProfile = await userProfileRepository.GetByAuthUserIdAsync(
            message.UserId,
            includeDeleted: true,
            cancellationToken);
        if (existingProfile is not null)
        {
            return existingProfile;
        }

        var username = await SelectUsernameAsync(message.UserId, message.UserName, cancellationToken);
        var profile = new UserProfile
        {
            AuthUserId = message.UserId,
            Username = username,
            NormalizedUsername = usernameNormalizer.Normalize(username),
            DisplayName = BuildDisplayName(message, username),
            FollowersCount = 0,
            FollowingCount = 0,
            PostsCount = 0,
            IsVerified = false,
            AccountType = AccountType.Personal,
            ProfileVisibility = ProfileVisibility.Public,
            CreatedAt = clock.UtcDateTime
        };

        await userProfileRepository.AddAsync(profile, cancellationToken);
        await EnqueueSearchIndexEventAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishProfileSnapshotEventAsync("ProfileCreated", profile, cancellationToken);
        return profile;
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
            // Discovery snapshots are eventually consistent and should not block provisioning.
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

    private async Task EnqueueSearchIndexEventAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;
        var payload = new SearchDocumentIndexRequestedV1(
            EventId: eventId,
            Document: SearchUserDocumentFactory.Build(profile, clock.UtcNow),
            CorrelationId: null,
            CausationId: profile.AuthUserId.ToString("D"),
            OccurredAtUtc: occurredAtUtc);

        await integrationEventOutbox.AddAsync(
            eventId,
            SearchDocumentIndexRequestedV1.EventName,
            SearchDocumentIndexRequestedV1.EventVersion,
            SearchDocumentIndexRequestedV1.RoutingKey,
            "Norge360.Accounts",
            payload,
            correlationId: null,
            traceId: null,
            occurredAtUtc,
            cancellationToken);
    }

    private async Task<string> SelectUsernameAsync(
        Guid authUserId,
        string? requestedUsername,
        CancellationToken cancellationToken)
    {
        var requestedAvailability = await usernameAvailabilityService.CheckAsync(
            requestedUsername,
            cancellationToken: cancellationToken);
        if (requestedAvailability.IsAvailable)
        {
            return requestedAvailability.Username;
        }

        var identifier = authUserId.ToString("N");
        foreach (var identifierLength in FallbackIdentifierLengths)
        {
            var fallback = $"user-{identifier[..identifierLength]}";
            var availability = await usernameAvailabilityService.CheckAsync(
                fallback,
                cancellationToken: cancellationToken);
            if (availability.IsAvailable)
            {
                return availability.Username;
            }
        }

        throw new InvalidOperationException(
            $"No deterministic fallback username is available for auth user '{authUserId:D}'.");
    }

    private static string BuildDisplayName(UserRegisteredV1 message, string fallbackUsername)
    {
        var nameParts = new[] { message.FirstName, message.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        var displayName = string.Join(" ", nameParts);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(message.UserName)
                ? fallbackUsername
                : message.UserName.Trim();
        }

        return displayName.Length <= MaximumDisplayNameLength
            ? displayName
            : displayName[..MaximumDisplayNameLength];
    }
}
