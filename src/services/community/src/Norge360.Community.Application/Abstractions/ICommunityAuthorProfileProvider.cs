// <copyright file="ICommunityAuthorProfileProvider.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Application.Models;

namespace Norge360.Community.Application.Abstractions;

public interface ICommunityAuthorProfileProvider
{
    Task<IReadOnlyDictionary<Guid, CommunityAuthorSummary>> GetAuthorSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid? currentUserId,
        CancellationToken cancellationToken);
}
