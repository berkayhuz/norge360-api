namespace Norge360.Discovery.Contracts.Responses;

public sealed record DiscoverySnapshotBatchUpsertResponse(int Accepted, int Created, int Updated, int Invalid);
