// <copyright file="UserSearchReindexServiceTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Moq;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Services;
using Norge360.Accounts.Domain.Entities;
using Norge360.Clock;
using Norge360.Search.Contracts.IntegrationEvents.V1;
using Xunit;

namespace Norge360.Accounts.Application.UnitTests;

public sealed class UserSearchReindexServiceTests
{
    [Fact]
    public async Task EnqueueAllActiveUsersAsync_ShouldPublishAllUsersInBatches()
    {
        var batch1 = new[]
        {
            CreateProfile(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-2)),
            CreateProfile(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1))
        };
        var batch2 = new[]
        {
            CreateProfile(Guid.NewGuid(), DateTime.UtcNow)
        };

        var profileRepo = new Mock<IUserProfileRepository>();
        profileRepo.SetupSequence(x => x.ListActiveProfilesForReindexBatchAsync(It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch1)
            .ReturnsAsync(batch2)
            .ReturnsAsync([]);

        var outbox = new Mock<IIntegrationEventOutbox>();
        var uow = new Mock<IAccountsUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
        clock.SetupGet(x => x.UtcDateTime).Returns(DateTime.UtcNow);

        var sut = new UserSearchReindexService(profileRepo.Object, outbox.Object, uow.Object, clock.Object);
        var count = await sut.EnqueueAllActiveUsersAsync(100, CancellationToken.None);

        count.Should().Be(3);
        outbox.Verify(x => x.AddAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<SearchDocumentIndexRequestedV1>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static UserProfile CreateProfile(Guid authUserId, DateTime createdAtUtc) =>
        new()
        {
            AuthUserId = authUserId,
            Username = $"user-{Guid.NewGuid():N}".Substring(0, 12),
            NormalizedUsername = $"user-{Guid.NewGuid():N}".Substring(0, 12),
            CreatedAt = createdAtUtc
        };
}
