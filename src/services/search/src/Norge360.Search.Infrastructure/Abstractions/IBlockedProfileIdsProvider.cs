// <copyright file="IBlockedProfileIdsProvider.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Infrastructure.Abstractions;

internal interface IBlockedProfileIdsProvider
{
    Task<IReadOnlySet<Guid>> GetRelatedBlockedProfileIdsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken);
}
