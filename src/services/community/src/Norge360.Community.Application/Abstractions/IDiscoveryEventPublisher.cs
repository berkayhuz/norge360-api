namespace Norge360.Community.Application.Abstractions;

public interface IDiscoveryEventPublisher
{
    Task PublishAsync(DiscoveryEventEnvelope discoveryEvent, CancellationToken cancellationToken = default);
}

public sealed record DiscoveryEventEnvelope(
    string EventType,
    string SourceService,
    string SourceEntityType,
    string SourceEntityId,
    Guid? ActorUserId,
    Guid? ActorProfileId,
    Guid? TargetUserId,
    Guid? TargetProfileId,
    string? TargetEntityType,
    string? TargetEntityId,
    string DeduplicationKey,
    DateTime? OccurredAt,
    string? MetadataJson);
