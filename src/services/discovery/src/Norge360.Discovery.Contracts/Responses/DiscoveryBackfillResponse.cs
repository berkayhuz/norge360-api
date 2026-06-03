namespace Norge360.Discovery.Contracts.Responses;

public sealed record DiscoveryBackfillResponse(int Processed, int Created, int Updated, int Invalid, int Batches);

public sealed record DiscoveryRankingRecomputeResponse(bool Recomputed);
