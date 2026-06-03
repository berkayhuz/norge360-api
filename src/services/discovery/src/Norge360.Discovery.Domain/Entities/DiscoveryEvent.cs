using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Domain.Entities;

public sealed class DiscoveryEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DiscoveryEventType EventType { get; set; }
    public string SourceService { get; set; } = null!;
    public string SourceEntityType { get; set; } = null!;
    public string SourceEntityId { get; set; } = null!;
    public Guid? ActorUserId { get; set; }
    public Guid? ActorProfileId { get; set; }
    public Guid? TargetUserId { get; set; }
    public Guid? TargetProfileId { get; set; }
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public int Points { get; set; }
    public string DeduplicationKey { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsValid { get; set; } = true;
    public string? InvalidReason { get; set; }
    public string? MetadataJson { get; set; }
}
