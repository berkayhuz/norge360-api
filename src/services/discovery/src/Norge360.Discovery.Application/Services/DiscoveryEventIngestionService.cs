using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;
using Norge360.Discovery.Domain.Entities;
using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Application.Services;

public sealed class DiscoveryEventIngestionService(
    IDiscoveryDbContext dbContext,
    ILogger<DiscoveryEventIngestionService>? logger = null) : IDiscoveryEventIngestionService
{
    public Task<DiscoveryEventIngestionResponse> IngestAsync(DiscoveryEventRequest request, CancellationToken cancellationToken = default)
        => IngestBatchAsync(new DiscoveryEventBatchRequest([request]), cancellationToken);

    public async Task<DiscoveryEventIngestionResponse> IngestBatchAsync(DiscoveryEventBatchRequest request, CancellationToken cancellationToken = default)
    {
        var accepted = 0;
        var duplicates = 0;
        var invalid = 0;

        foreach (var item in request.Events.Take(200))
        {
            var normalized = Normalize(item);
            if (normalized is null)
            {
                invalid++;
                logger?.LogWarning(
                    "Invalid discovery event rejected. EventType={EventType} SourceService={SourceService} SourceEntityType={SourceEntityType}",
                    item.EventType,
                    item.SourceService,
                    item.SourceEntityType);
                continue;
            }

            if (await dbContext.DiscoveryEvents.AnyAsync(x => x.DeduplicationKey == normalized.DeduplicationKey, cancellationToken))
            {
                duplicates++;
                logger?.LogInformation(
                    "Duplicate discovery event ignored. EventType={EventType} SourceService={SourceService} SourceEntityType={SourceEntityType}",
                    normalized.EventType,
                    normalized.SourceService,
                    normalized.SourceEntityType);
                continue;
            }

            if (await IsDuplicateProfileViewAsync(normalized, cancellationToken))
            {
                normalized.IsValid = false;
                normalized.Points = 0;
                normalized.InvalidReason = "duplicate_daily_profile_view";
                logger?.LogInformation(
                    "Duplicate daily profile view ignored for scoring. ActorUserId={ActorUserId} TargetProfileId={TargetProfileId}",
                    normalized.ActorUserId,
                    normalized.TargetProfileId);
            }

            if (normalized.EventType is DiscoveryEventType.ProfileUnfollowed or DiscoveryEventType.PostUnliked or DiscoveryEventType.PostCommentDeleted)
            {
                await InvalidateSourceEventAsync(normalized, cancellationToken);
            }

            dbContext.DiscoveryEvents.Add(normalized);
            await ApplySubjectSnapshotEventAsync(normalized, cancellationToken);
            await ApplyAggregateAsync(normalized, cancellationToken);
            accepted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DiscoveryEventIngestionResponse(accepted, duplicates, invalid);
    }

    private DiscoveryEvent? Normalize(DiscoveryEventRequest request)
    {
        if (!Enum.TryParse<DiscoveryEventType>(request.EventType, true, out var eventType) || eventType == DiscoveryEventType.Unknown)
        {
            return null;
        }

        var sourceService = NormalizeRequired(request.SourceService, 80);
        var sourceEntityType = NormalizeRequired(request.SourceEntityType, 80);
        var sourceEntityId = NormalizeRequired(request.SourceEntityId, 128);
        var deduplicationKey = NormalizeRequired(request.DeduplicationKey, 256);
        if (sourceService is null || sourceEntityType is null || sourceEntityId is null || deduplicationKey is null)
        {
            return null;
        }

        var occurredAt = DateTime.SpecifyKind(request.OccurredAt ?? DateTime.UtcNow, DateTimeKind.Utc);
        var points = CalculatePoints(eventType, request);
        var isValid = points != 0 || IsStateOnlyEvent(eventType);
        var invalidReason = isValid ? null : "event_not_scoreable";

        if (request.ActorUserId.HasValue && request.TargetUserId.HasValue && request.ActorUserId.Value == request.TargetUserId.Value)
        {
            points = 0;
            isValid = false;
            invalidReason = "self_interaction";
        }

        if (eventType == DiscoveryEventType.ProfileViewed && (!request.ActorUserId.HasValue || !request.TargetProfileId.HasValue))
        {
            points = 0;
            isValid = false;
            invalidReason = "profile_view_requires_authenticated_actor";
        }

        return new DiscoveryEvent
        {
            EventType = eventType,
            SourceService = sourceService,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            ActorUserId = request.ActorUserId,
            ActorProfileId = request.ActorProfileId,
            TargetUserId = request.TargetUserId,
            TargetProfileId = request.TargetProfileId,
            TargetEntityType = NormalizeOptional(request.TargetEntityType, 80),
            TargetEntityId = NormalizeOptional(request.TargetEntityId, 128),
            Points = points,
            DeduplicationKey = deduplicationKey,
            OccurredAt = occurredAt,
            ReceivedAt = DateTime.UtcNow,
            IsValid = isValid,
            InvalidReason = invalidReason,
            MetadataJson = NormalizeOptional(request.MetadataJson, 4096)
        };
    }

    private async Task<bool> IsDuplicateProfileViewAsync(DiscoveryEvent discoveryEvent, CancellationToken cancellationToken)
    {
        if (discoveryEvent.EventType != DiscoveryEventType.ProfileViewed ||
            !discoveryEvent.ActorUserId.HasValue ||
            !discoveryEvent.TargetProfileId.HasValue)
        {
            return false;
        }

        var start = discoveryEvent.OccurredAt.Date;
        var end = start.AddDays(1);
        return await dbContext.DiscoveryEvents.AnyAsync(
            x => x.EventType == DiscoveryEventType.ProfileViewed &&
                 x.ActorUserId == discoveryEvent.ActorUserId &&
                 x.TargetProfileId == discoveryEvent.TargetProfileId &&
                 x.IsValid &&
                 x.OccurredAt >= start &&
                 x.OccurredAt < end,
            cancellationToken);
    }

    private async Task ApplyAggregateAsync(DiscoveryEvent discoveryEvent, CancellationToken cancellationToken)
    {
        var targetId = discoveryEvent.TargetProfileId ?? discoveryEvent.TargetUserId;
        if (!discoveryEvent.IsValid || discoveryEvent.Points == 0 || !targetId.HasValue)
        {
            return;
        }

        var date = DateOnly.FromDateTime(discoveryEvent.OccurredAt);
        var aggregate = await dbContext.DiscoveryDailyAggregates.FirstOrDefaultAsync(
            x => x.TargetType == DiscoverySubjectType.User && x.TargetId == targetId.Value && x.Date == date,
            cancellationToken);

        if (aggregate is null)
        {
            aggregate = new DiscoveryDailyAggregate
            {
                TargetType = DiscoverySubjectType.User,
                TargetId = targetId.Value,
                Date = date,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.DiscoveryDailyAggregates.Add(aggregate);
        }

        switch (discoveryEvent.EventType)
        {
            case DiscoveryEventType.ProfileFollowed:
                aggregate.FollowPoints += discoveryEvent.Points;
                break;
            case DiscoveryEventType.ProfileViewed:
                aggregate.ProfileViewPoints += discoveryEvent.Points;
                break;
            case DiscoveryEventType.PostLiked:
                aggregate.LikePoints += discoveryEvent.Points;
                break;
            case DiscoveryEventType.PostCommented:
                aggregate.CommentPoints += discoveryEvent.Points;
                break;
        }

        aggregate.RawScore = aggregate.FollowPoints + aggregate.ProfileViewPoints + aggregate.LikePoints + aggregate.CommentPoints - aggregate.NegativePoints;
        aggregate.UpdatedAt = DateTime.UtcNow;
    }

    private async Task InvalidateSourceEventAsync(DiscoveryEvent reversingEvent, CancellationToken cancellationToken)
    {
        var sourceType = reversingEvent.EventType switch
        {
            DiscoveryEventType.ProfileUnfollowed => DiscoveryEventType.ProfileFollowed,
            DiscoveryEventType.PostUnliked => DiscoveryEventType.PostLiked,
            _ => DiscoveryEventType.PostCommented
        };

        var source = await dbContext.DiscoveryEvents
            .Where(x => x.EventType == sourceType &&
                        x.SourceEntityType == reversingEvent.SourceEntityType &&
                        x.SourceEntityId == reversingEvent.SourceEntityId &&
                        x.ActorUserId == reversingEvent.ActorUserId &&
                        x.IsValid)
            .OrderByDescending(x => x.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            return;
        }

        source.IsValid = false;
        source.InvalidReason = reversingEvent.EventType switch
        {
            DiscoveryEventType.ProfileUnfollowed => "profile_unfollowed",
            DiscoveryEventType.PostUnliked => "post_unliked",
            _ => "comment_deleted"
        };

        var targetId = source.TargetProfileId ?? source.TargetUserId;
        if (!targetId.HasValue || source.Points == 0)
        {
            return;
        }

        var date = DateOnly.FromDateTime(source.OccurredAt);
        var aggregate = await dbContext.DiscoveryDailyAggregates.FirstOrDefaultAsync(
            x => x.TargetType == DiscoverySubjectType.User && x.TargetId == targetId.Value && x.Date == date,
            cancellationToken);
        if (aggregate is null)
        {
            return;
        }

        if (source.EventType == DiscoveryEventType.ProfileFollowed)
        {
            aggregate.FollowPoints = Math.Max(0, aggregate.FollowPoints - source.Points);
        }
        else if (source.EventType == DiscoveryEventType.PostLiked)
        {
            aggregate.LikePoints = Math.Max(0, aggregate.LikePoints - source.Points);
        }
        else if (source.EventType == DiscoveryEventType.PostCommented)
        {
            aggregate.CommentPoints = Math.Max(0, aggregate.CommentPoints - source.Points);
        }

        aggregate.RawScore = aggregate.FollowPoints + aggregate.ProfileViewPoints + aggregate.LikePoints + aggregate.CommentPoints - aggregate.NegativePoints;
        aggregate.UpdatedAt = DateTime.UtcNow;
    }

    private async Task ApplySubjectSnapshotEventAsync(DiscoveryEvent discoveryEvent, CancellationToken cancellationToken)
    {
        if (discoveryEvent.EventType is not (
            DiscoveryEventType.ProfileCreated
            or DiscoveryEventType.ProfileUpdated
            or DiscoveryEventType.ProfileVisibilityChanged
            or DiscoveryEventType.ProfileDeleted
            or DiscoveryEventType.ProfileDeactivated
            or DiscoveryEventType.ProfileReactivated))
        {
            return;
        }

        if (!discoveryEvent.TargetProfileId.HasValue)
        {
            return;
        }

        var snapshot = await dbContext.DiscoverySubjectSnapshots.FirstOrDefaultAsync(
            x => x.SubjectType == DiscoverySubjectType.User && x.SubjectId == discoveryEvent.TargetProfileId.Value,
            cancellationToken);
        if (snapshot is null)
        {
            snapshot = new DiscoverySubjectSnapshot { SubjectType = DiscoverySubjectType.User, SubjectId = discoveryEvent.TargetProfileId.Value };
            dbContext.DiscoverySubjectSnapshots.Add(snapshot);
        }

        snapshot.AuthUserId = discoveryEvent.TargetUserId ?? snapshot.AuthUserId;
        snapshot.UpdatedAt = DateTime.UtcNow;

        if (discoveryEvent.EventType is DiscoveryEventType.ProfileDeleted or DiscoveryEventType.ProfileDeactivated)
        {
            snapshot.IsActive = false;
            snapshot.IsDeleted = true;
            return;
        }

        if (discoveryEvent.EventType == DiscoveryEventType.ProfileReactivated)
        {
            snapshot.IsActive = true;
            snapshot.IsDeleted = false;
        }

        if (string.IsNullOrWhiteSpace(discoveryEvent.MetadataJson))
        {
            return;
        }

        using var document = JsonDocument.Parse(discoveryEvent.MetadataJson);
        var root = document.RootElement;
        snapshot.Username = ReadString(root, "username") ?? snapshot.Username;
        snapshot.DisplayName = ReadString(root, "displayName") ?? snapshot.DisplayName;
        snapshot.AvatarUrl = ReadString(root, "avatarUrl") ?? snapshot.AvatarUrl;
        snapshot.Bio = ReadString(root, "bio") ?? snapshot.Bio;
        snapshot.FollowersCount = ReadInt(root, "followersCount") ?? snapshot.FollowersCount;
        snapshot.PostsCount = ReadInt(root, "postsCount") ?? snapshot.PostsCount;
        snapshot.Visibility = ReadString(root, "visibility") ?? snapshot.Visibility;
        snapshot.IsVerified = ReadBool(root, "isVerified") ?? snapshot.IsVerified;
        snapshot.IsActive = ReadBool(root, "isActive") ?? snapshot.IsActive;
        snapshot.IsDeleted = ReadBool(root, "isDeleted") ?? snapshot.IsDeleted;
    }

    private static int CalculatePoints(DiscoveryEventType eventType, DiscoveryEventRequest request) =>
        eventType switch
        {
            DiscoveryEventType.ProfileFollowed => 3,
            DiscoveryEventType.ProfileViewed => 1,
            DiscoveryEventType.PostLiked => 1,
            DiscoveryEventType.PostCommented when IsValidComment(request.MetadataJson) => 2,
            _ => 0
        };

    private static bool IsValidComment(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var body = ReadString(document.RootElement, "body") ?? ReadString(document.RootElement, "commentBody");
            if (body is null)
            {
                return true;
            }

            var trimmed = body.Trim();
            return trimmed.Length >= 3 && trimmed.Any(char.IsLetterOrDigit);
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static bool IsStateOnlyEvent(DiscoveryEventType eventType) =>
        eventType is DiscoveryEventType.ProfileUpdated
            or DiscoveryEventType.ProfileCreated
            or DiscoveryEventType.ProfileVisibilityChanged
            or DiscoveryEventType.ProfileDeleted
            or DiscoveryEventType.ProfileDeactivated
            or DiscoveryEventType.ProfileReactivated
            or DiscoveryEventType.ProfileUnfollowed
            or DiscoveryEventType.ProfileBlocked
            or DiscoveryEventType.ProfileUnblocked
            or DiscoveryEventType.PostCreated
            or DiscoveryEventType.PostUnliked
            or DiscoveryEventType.PostCommentDeleted
            or DiscoveryEventType.PostDeleted
            or DiscoveryEventType.PostHidden
            or DiscoveryEventType.PostModerated;

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool? ReadBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    private static int? ReadInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : null;

    private static string? NormalizeRequired(string? value, int maxLength)
    {
        var normalized = NormalizeOptional(value, maxLength);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
