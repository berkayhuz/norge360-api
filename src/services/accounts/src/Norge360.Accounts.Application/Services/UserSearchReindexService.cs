// <copyright file="UserSearchReindexService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Clock;
using Norge360.Search.Contracts.IntegrationEvents.V1;

namespace Norge360.Accounts.Application.Services;

public sealed class UserSearchReindexService(
    IUserProfileRepository userProfileRepository,
    IIntegrationEventOutbox integrationEventOutbox,
    IAccountsUnitOfWork unitOfWork,
    IClock clock) : IUserSearchReindexService
{
    public async Task<int> EnqueueAllActiveUsersAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 10, 500);
        var total = 0;
        DateTime? lastCreatedAt = null;
        Guid? lastProfileId = null;

        while (true)
        {
            var profiles = await userProfileRepository.ListActiveProfilesForReindexBatchAsync(
                lastCreatedAt,
                lastProfileId,
                safeBatchSize,
                cancellationToken);
            if (profiles.Count == 0)
            {
                break;
            }

            foreach (var profile in profiles)
            {
                var eventId = Guid.NewGuid();
                var occurredAtUtc = clock.UtcDateTime;
                var payload = new SearchDocumentIndexRequestedV1(
                    EventId: eventId,
                    Document: SearchUserDocumentFactory.Build(profile, clock.UtcNow),
                    CorrelationId: null,
                    CausationId: profile.AuthUserId.ToString("D"),
                    OccurredAtUtc: occurredAtUtc);

                await integrationEventOutbox.AddAsync(
                    eventId,
                    SearchDocumentIndexRequestedV1.EventName,
                    SearchDocumentIndexRequestedV1.EventVersion,
                    SearchDocumentIndexRequestedV1.RoutingKey,
                    "Norge360.Accounts",
                    payload,
                    correlationId: null,
                    traceId: null,
                    occurredAtUtc,
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            total += profiles.Count;

            var last = profiles.Last();
            lastCreatedAt = last.CreatedAt;
            lastProfileId = last.Id;
        }

        return total;
    }
}
