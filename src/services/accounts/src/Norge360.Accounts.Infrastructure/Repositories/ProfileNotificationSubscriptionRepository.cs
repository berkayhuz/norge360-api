// <copyright file="ProfileNotificationSubscriptionRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Infrastructure.Persistence;

namespace Norge360.Accounts.Infrastructure.Repositories;

public sealed class ProfileNotificationSubscriptionRepository(AccountsDbContext dbContext) : IProfileNotificationSubscriptionRepository
{
    public Task<UserProfileNotificationSubscription?> GetAsync(
        Guid subscriberProfileId,
        Guid targetProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserProfileNotificationSubscriptions.FirstOrDefaultAsync(
            item => item.SubscriberProfileId == subscriberProfileId && item.TargetProfileId == targetProfileId,
            cancellationToken);

    public Task<bool> ExistsAsync(
        Guid subscriberProfileId,
        Guid targetProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.UserProfileNotificationSubscriptions
            .AsNoTracking()
            .AnyAsync(
                item => item.SubscriberProfileId == subscriberProfileId && item.TargetProfileId == targetProfileId,
                cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> ListSubscriberAuthUserIdsAsync(
        Guid targetProfileId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 1000);
        return await dbContext.UserProfileNotificationSubscriptions
            .AsNoTracking()
            .Where(item => item.TargetProfileId == targetProfileId)
            .Join(
                dbContext.UserProfiles.AsNoTracking().Where(profile => !profile.IsDeleted && profile.IsActive),
                item => item.SubscriberProfileId,
                profile => profile.Id,
                (_, profile) => profile.AuthUserId)
            .Distinct()
            .Take(safeLimit)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(
        UserProfileNotificationSubscription subscription,
        CancellationToken cancellationToken = default) =>
        dbContext.UserProfileNotificationSubscriptions.AddAsync(subscription, cancellationToken).AsTask();

    public void Remove(UserProfileNotificationSubscription subscription) =>
        dbContext.UserProfileNotificationSubscriptions.Remove(subscription);
}
