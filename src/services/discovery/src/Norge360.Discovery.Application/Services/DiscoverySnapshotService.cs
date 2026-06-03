// <copyright file="DiscoverySnapshotService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;
using Norge360.Discovery.Domain.Entities;
using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Application.Services;

public sealed class DiscoverySnapshotService(
    IDiscoveryDbContext dbContext,
    ILogger<DiscoverySnapshotService>? logger = null) : IDiscoverySnapshotService
{
    public async Task<DiscoverySnapshotBatchUpsertResponse> UpsertBatchAsync(
        DiscoverySnapshotBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var invalid = 0;
        var accepted = 0;

        foreach (var item in request.Snapshots.Take(500))
        {
            if (item.ProfileId == Guid.Empty || string.IsNullOrWhiteSpace(item.Username))
            {
                invalid++;
                logger?.LogWarning("Invalid discovery snapshot rejected. ProfileId={ProfileId}", item.ProfileId);
                continue;
            }

            var snapshot = await dbContext.DiscoverySubjectSnapshots.FirstOrDefaultAsync(
                x => x.SubjectType == DiscoverySubjectType.User && x.SubjectId == item.ProfileId,
                cancellationToken);

            if (snapshot is null)
            {
                snapshot = new DiscoverySubjectSnapshot
                {
                    SubjectType = DiscoverySubjectType.User,
                    SubjectId = item.ProfileId
                };
                dbContext.DiscoverySubjectSnapshots.Add(snapshot);
                created++;
            }
            else
            {
                updated++;
            }

            snapshot.AuthUserId = item.AuthUserId;
            snapshot.Username = item.Username.Trim();
            snapshot.DisplayName = NormalizeOptional(item.DisplayName);
            snapshot.AvatarUrl = NormalizeOptional(item.AvatarUrl);
            snapshot.Bio = NormalizeOptional(item.Bio);
            snapshot.IsVerified = item.IsVerified;
            snapshot.Visibility = string.IsNullOrWhiteSpace(item.Visibility) ? "Public" : item.Visibility.Trim();
            snapshot.IsActive = item.IsActive;
            snapshot.IsDeleted = item.IsDeleted;
            snapshot.FollowersCount = Math.Max(0, item.FollowersCount);
            snapshot.PostsCount = Math.Max(0, item.PostsCount);
            snapshot.UpdatedAt = item.UpdatedAt.UtcDateTime;
            accepted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DiscoverySnapshotBatchUpsertResponse(accepted, created, updated, invalid);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
