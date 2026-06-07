// <copyright file="IProfileNotificationSubscriptionRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Domain.Entities;

namespace Norge360.Accounts.Application.Abstractions;

public interface IProfileNotificationSubscriptionRepository
{
    Task<UserProfileNotificationSubscription?> GetAsync(
        Guid subscriberProfileId,
        Guid targetProfileId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid subscriberProfileId,
        Guid targetProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> ListSubscriberAuthUserIdsAsync(
        Guid targetProfileId,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserProfileNotificationSubscription subscription,
        CancellationToken cancellationToken = default);

    void Remove(UserProfileNotificationSubscription subscription);
}
