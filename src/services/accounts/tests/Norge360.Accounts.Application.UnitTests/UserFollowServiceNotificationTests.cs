// <copyright file="UserFollowServiceNotificationTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Moq;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Application.Services;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;
using Xunit;

namespace Norge360.Accounts.Application.UnitTests;

public sealed class UserFollowServiceNotificationTests
{
    [Fact]
    public async Task FollowByUsernameAsync_WhenProfileIsPrivate_ShouldCreatePendingRequestAndNotifyFollowee()
    {
        var fixture = new FollowFixture();
        var follower = CreateProfile("follower");
        var followee = CreateProfile("followee", ProfileVisibility.Private);
        UserFollow? addedFollow = null;
        fixture.Profiles.Setup(x => x.GetTrackedByAuthUserIdAsync(follower.AuthUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(follower);
        fixture.Profiles.Setup(x => x.GetTrackedByNormalizedUsernameAsync("FOLLOWEE", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(followee);
        fixture.Follows.Setup(x => x.GetAsync(follower.Id, followee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFollow?)null);
        fixture.Follows.Setup(x => x.AddAsync(It.IsAny<UserFollow>(), It.IsAny<CancellationToken>()))
            .Callback<UserFollow, CancellationToken>((follow, _) => addedFollow = follow)
            .Returns(Task.CompletedTask);
        fixture.Follows.Setup(x => x.CountFollowersAsync(followee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => followee.FollowersCount);
        fixture.Follows.Setup(x => x.CountFollowingAsync(followee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => followee.FollowingCount);

        var result = await fixture.Service.FollowByUsernameAsync(follower.AuthUserId, "followee", CancellationToken.None);

        result.Status.Should().Be(UserFollowMutationStatus.Success);
        result.IsFollowing.Should().BeFalse();
        result.IsFollowRequestPending.Should().BeTrue();
        addedFollow.Should().NotBeNull();
        addedFollow!.Status.Should().Be(FollowStatus.Pending);
        follower.FollowingCount.Should().Be(0);
        followee.FollowersCount.Should().Be(0);
        fixture.Notifications.Verify(
            x => x.PublishFollowRequestAsync(follower, followee, addedFollow.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Notifications.Verify(
            x => x.PublishFollowedAsync(It.IsAny<UserProfile>(), It.IsAny<UserProfile>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Discovery.Verify(
            x => x.PublishAsync(It.IsAny<DiscoveryEventEnvelope>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptFollowRequestByUsernameAsync_WhenPending_ShouldActivateAndNotifyFollower()
    {
        var fixture = new FollowFixture();
        var follower = CreateProfile("follower");
        var followee = CreateProfile("followee");
        var pendingFollow = new UserFollow
        {
            FollowerId = follower.Id,
            FolloweeId = followee.Id,
            Status = FollowStatus.Pending
        };
        fixture.Profiles.Setup(x => x.GetTrackedByAuthUserIdAsync(followee.AuthUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(followee);
        fixture.Profiles.Setup(x => x.GetTrackedByNormalizedUsernameAsync("FOLLOWER", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(follower);
        fixture.Follows.Setup(x => x.GetAsync(follower.Id, followee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingFollow);
        fixture.Follows.Setup(x => x.CountFollowersAsync(followee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => followee.FollowersCount);
        fixture.Follows.Setup(x => x.CountFollowingAsync(followee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => followee.FollowingCount);

        var result = await fixture.Service.AcceptFollowRequestByUsernameAsync(followee.AuthUserId, "follower", CancellationToken.None);

        result.Status.Should().Be(UserFollowMutationStatus.Success);
        result.IsFollowing.Should().BeTrue();
        result.IsFollowRequestPending.Should().BeFalse();
        pendingFollow.Status.Should().Be(FollowStatus.Active);
        follower.FollowingCount.Should().Be(1);
        followee.FollowersCount.Should().Be(1);
        fixture.Notifications.Verify(
            x => x.PublishFollowRequestAcceptedAsync(follower, followee, pendingFollow.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Discovery.Verify(
            x => x.PublishAsync(It.Is<DiscoveryEventEnvelope>(e => e.EventType == "ProfileFollowed"), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UserProfile CreateProfile(string username, ProfileVisibility visibility = ProfileVisibility.Public) =>
        new()
        {
            AuthUserId = Guid.NewGuid(),
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            ProfileVisibility = visibility
        };

    private sealed class FollowFixture
    {
        public FollowFixture()
        {
            Validator.Setup(x => x.Validate(It.IsAny<string?>())).Returns(UsernameValidationResult.Valid());
            Normalizer.Setup(x => x.Normalize(It.IsAny<string?>()))
                .Returns((string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty);
            Blocks.Setup(x => x.ExistsBetweenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            UnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            Discovery.Setup(x => x.PublishAsync(It.IsAny<DiscoveryEventEnvelope>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notifications.Setup(x => x.PublishFollowRequestAsync(It.IsAny<UserProfile>(), It.IsAny<UserProfile>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notifications.Setup(x => x.PublishFollowedAsync(It.IsAny<UserProfile>(), It.IsAny<UserProfile>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notifications.Setup(x => x.PublishFollowRequestAcceptedAsync(It.IsAny<UserProfile>(), It.IsAny<UserProfile>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Service = new UserFollowService(
                UnitOfWork.Object,
                Follows.Object,
                Blocks.Object,
                Profiles.Object,
                Normalizer.Object,
                Validator.Object,
                Discovery.Object,
                Notifications.Object,
                ProfileNotifications.Object);
        }

        public Mock<IAccountsUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IUserFollowRepository> Follows { get; } = new();
        public Mock<IUserBlockRepository> Blocks { get; } = new();
        public Mock<IUserProfileRepository> Profiles { get; } = new();
        public Mock<IUsernameNormalizer> Normalizer { get; } = new();
        public Mock<IUsernameValidator> Validator { get; } = new();
        public Mock<IDiscoveryEventPublisher> Discovery { get; } = new();
        public Mock<IAccountNotificationPublisher> Notifications { get; } = new();
        public Mock<IProfileNotificationSubscriptionRepository> ProfileNotifications { get; } = new();
        public UserFollowService Service { get; }
    }
}
