// <copyright file="CommunityNotificationServices.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;
using Norge360.Messaging.Abstractions;
using Norge360.Messaging.RabbitMq.Options;
using Norge360.Notification.Contracts.IntegrationEvents.V1;
using Norge360.Notification.Contracts.Notifications;
using Norge360.Notification.Contracts.Notifications.Enums;
using Norge360.Notification.Contracts.Notifications.Models;

namespace Norge360.Community.Infrastructure.Services;

internal sealed class AccountsCommunityNotificationTargetProvider(
    IHttpClientFactory httpClientFactory,
    IInternalServiceRequestSigner requestSigner,
    ILogger<AccountsCommunityNotificationTargetProvider> logger) : ICommunityNotificationTargetProvider
{
    public async Task<CommunityNotificationTargets> ResolveAsync(
        Guid authorUserId,
        string? city,
        bool includeFollowers,
        bool includeProfileSubscribers,
        bool includeCityResidents,
        int maxRecipients,
        CancellationToken cancellationToken = default)
    {
        if (authorUserId == Guid.Empty)
        {
            return EmptyTargets();
        }

        try
        {
            var client = httpClientFactory.CreateClient("accounts-community");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/internal/users/community-notification-targets")
            {
                Content = JsonContent.Create(new
                {
                    authorUserId,
                    city,
                    includeFollowers,
                    includeProfileSubscribers,
                    includeCityResidents,
                    maxRecipients
                })
            };

            await requestSigner.SignAsync(request, cancellationToken);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Accounts community notification target request failed with status {StatusCode}. AuthorUserId={AuthorUserId}",
                    response.StatusCode,
                    authorUserId);
                return EmptyTargets();
            }

            var payload = await response.Content.ReadFromJsonAsync<InternalCommunityNotificationTargetsResponse>(
                cancellationToken: cancellationToken);
            if (payload is null)
            {
                return EmptyTargets();
            }

            return new CommunityNotificationTargets(
                Normalize(payload.Followers),
                Normalize(payload.ProfileSubscribers),
                Normalize(payload.CityResidents));
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to resolve community notification targets. AuthorUserId={AuthorUserId}",
                authorUserId);
            return EmptyTargets();
        }
    }

    private static CommunityNotificationTargets EmptyTargets() => new([], [], []);

    private static IReadOnlyCollection<Guid> Normalize(IReadOnlyCollection<Guid>? userIds) =>
        userIds?
            .Where(static userId => userId != Guid.Empty)
            .Distinct()
            .ToArray() ?? [];

    private sealed record InternalCommunityNotificationTargetsResponse(
        IReadOnlyCollection<Guid> Followers,
        IReadOnlyCollection<Guid> ProfileSubscribers,
        IReadOnlyCollection<Guid> CityResidents);
}

internal sealed class RabbitMqCommunityNotificationPublisher(
    ICommunityNotificationTargetProvider targetProvider,
    IIntegrationEventPublisher eventPublisher,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<RabbitMqCommunityNotificationPublisher> logger) : ICommunityNotificationPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const int MaxPostFanoutRecipients = 500;

    public async Task PublishPostCreatedAsync(
        CommunityPostPublishedNotification notification,
        CancellationToken cancellationToken = default)
    {
        var targets = await targetProvider.ResolveAsync(
            notification.Author.UserId,
            notification.City,
            includeFollowers: notification.IsFirstPost,
            includeProfileSubscribers: true,
            includeCityResidents: !string.IsNullOrWhiteSpace(notification.City),
            maxRecipients: MaxPostFanoutRecipients,
            cancellationToken);

        var sent = new HashSet<Guid>();
        await PublishPostGroupAsync(
            targets.ProfileSubscribers,
            sent,
            notification,
            NotificationTypes.ProfilePost,
            "Yeni gonderi",
            $"{DisplayName(notification.Author)} yeni bir gonderi paylasti.",
            cancellationToken);

        if (notification.IsFirstPost)
        {
            await PublishPostGroupAsync(
                targets.Followers,
                sent,
                notification,
                NotificationTypes.FollowedFirstPost,
                "Ilk gonderi",
                $"Takip ettigin {DisplayName(notification.Author)} ilk gonderisini paylasti.",
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(notification.City))
        {
            await PublishPostGroupAsync(
                targets.CityResidents,
                sent,
                notification,
                NotificationTypes.CityPost,
                $"{notification.City} icin yeni gonderi",
                $"{DisplayName(notification.Author)} {notification.City} icin yeni bir gonderi paylasti.",
                cancellationToken);
        }
    }

    public Task PublishPostLikedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default) =>
        PublishInteractionAsync(
            notification,
            NotificationTypes.PostLike,
            "Gonderin begenildi",
            $"{DisplayName(notification.Actor)} gonderini begendi.",
            $"community:post-like:{notification.EntityId:D}",
            cancellationToken);

    public Task PublishCommentLikedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default) =>
        PublishInteractionAsync(
            notification,
            NotificationTypes.CommentLike,
            "Yorumun begenildi",
            $"{DisplayName(notification.Actor)} yorumunu begendi.",
            $"community:comment-like:{notification.EntityId:D}",
            cancellationToken);

    public Task PublishPostCommentedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default) =>
        PublishInteractionAsync(
            notification,
            NotificationTypes.PostComment,
            "Gonderine yorum yapildi",
            $"{DisplayName(notification.Actor)} gonderine yorum yapti.",
            $"community:post-comment:{notification.EntityId:D}",
            cancellationToken);

    public Task PublishCommentRepliedAsync(
        CommunityInteractionNotification notification,
        CancellationToken cancellationToken = default) =>
        PublishInteractionAsync(
            notification,
            NotificationTypes.CommentReply,
            "Yorumuna yanit geldi",
            $"{DisplayName(notification.Actor)} yorumuna yanit verdi.",
            $"community:comment-reply:{notification.EntityId:D}",
            cancellationToken);

    private async Task PublishPostGroupAsync(
        IReadOnlyCollection<Guid> recipients,
        ISet<Guid> sent,
        CommunityPostPublishedNotification notification,
        string notificationType,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        foreach (var recipientUserId in recipients)
        {
            if (recipientUserId == Guid.Empty ||
                recipientUserId == notification.Author.UserId ||
                !sent.Add(recipientUserId))
            {
                continue;
            }

            await PublishAsync(
                recipientUserId,
                notification.Author,
                notificationType,
                subject,
                body,
                BuildPostUrl(notification.Author, notification.PostSlug),
                "CommunityPost",
                notification.PostId.ToString("D"),
                $"community:{notificationType}:{notification.PostId:D}:{recipientUserId:D}",
                notification.CreatedAtUtc,
                notification.Caption,
                cancellationToken);
        }
    }

    private Task PublishInteractionAsync(
        CommunityInteractionNotification notification,
        string notificationType,
        string subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (notification.RecipientUserId == Guid.Empty ||
            notification.RecipientUserId == notification.Actor.UserId)
        {
            return Task.CompletedTask;
        }

        return PublishAsync(
            notification.RecipientUserId,
            notification.Actor,
            notificationType,
            subject,
            body,
            BuildPostUrl(notification.PostAuthor, notification.PostSlug),
            notification.EntityType,
            notification.EntityId.ToString("D"),
            idempotencyKey,
            notification.OccurredAtUtc,
            notification.Text,
            cancellationToken);
    }

    private async Task PublishAsync(
        Guid recipientUserId,
        CommunityNotificationActor actor,
        string notificationType,
        string subject,
        string body,
        string url,
        string entityType,
        string entityId,
        string idempotencyKey,
        DateTime occurredAtUtc,
        string? text,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var safeOccurredAt = occurredAtUtc == DateTime.MinValue ? DateTime.UtcNow : occurredAtUtc;
        var metadata = new Dictionary<string, string>
        {
            ["notificationType"] = notificationType,
            ["url"] = url,
            ["actorUserId"] = actor.UserId.ToString("D"),
            ["entityType"] = entityType,
            ["entityId"] = entityId
        };

        AddIfPresent(metadata, "actorUsername", actor.Username);
        AddIfPresent(metadata, "actorDisplayName", DisplayName(actor));
        AddIfPresent(metadata, "actorAvatarUrl", actor.AvatarUrl);
        AddIfPresent(metadata, "excerpt", Trim(text, 160));

        var notification = new NotificationRequestedV1(
            eventId,
            recipientUserId,
            "Community",
            NotificationCategory.Community,
            NotificationPriority.Normal,
            new NotificationRecipient(recipientUserId, null, null, null, null),
            [NotificationChannel.InApp],
            subject,
            body,
            null,
            new NotificationTemplateData(notificationType, metadata),
            metadata,
            null,
            idempotencyKey,
            safeOccurredAt);

        var message = new IntegrationMessage(
            new IntegrationEventMetadata(
                eventId,
                NotificationRequestedV1.EventName,
                NotificationRequestedV1.EventVersion,
                "Community",
                safeOccurredAt,
                null,
                null),
            JsonSerializer.Serialize(notification, SerializerOptions));

        try
        {
            await eventPublisher.PublishAsync(
                rabbitMqOptions.Value.Exchange,
                NotificationRequestedV1.RoutingKey,
                message,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to publish community notification. Type={NotificationType} RecipientUserId={RecipientUserId}",
                notificationType,
                recipientUserId);
            throw;
        }
    }

    private static string BuildPostUrl(CommunityNotificationActor postAuthor, string postSlug) =>
        string.IsNullOrWhiteSpace(postAuthor.Username)
            ? "/feed"
            : $"/{postAuthor.Username}/feed/{postSlug}";

    private static string DisplayName(CommunityNotificationActor actor) =>
        !string.IsNullOrWhiteSpace(actor.DisplayName)
            ? actor.DisplayName.Trim()
            : !string.IsNullOrWhiteSpace(actor.Username)
                ? actor.Username.Trim()
                : "Bir kullanici";

    private static void AddIfPresent(IDictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
