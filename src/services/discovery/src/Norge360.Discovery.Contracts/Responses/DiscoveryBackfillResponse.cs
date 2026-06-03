// <copyright file="DiscoveryBackfillResponse.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Discovery.Contracts.Responses;

public sealed record DiscoveryBackfillResponse(int Processed, int Created, int Updated, int Invalid, int Batches);

public sealed record DiscoveryRankingRecomputeResponse(bool Recomputed);
