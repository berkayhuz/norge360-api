// <copyright file="DiscoveryEventRequests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

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
