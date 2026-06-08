// <copyright file="IUserRelationshipReader.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Application.Models;

namespace Norge360.MessagingService.Application.Abstractions;

public interface IUserRelationshipReader
{
    Task<MessagingRelationship> GetAsync(Guid requesterUserId, Guid targetUserId, CancellationToken cancellationToken = default);
}
