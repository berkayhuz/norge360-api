namespace Norge360.Discovery.Contracts.Requests;

public sealed record DiscoveryEventRequest(
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

public sealed record DiscoveryEventBatchRequest(IReadOnlyList<DiscoveryEventRequest> Events);
