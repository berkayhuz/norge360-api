// <copyright file="ProfileQueryServiceTests.cs" company="Norge360">
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

public sealed class ProfileQueryServiceTests
{
    [Fact]
    public async Task GetMyProfileAsync_ShouldUseFollowTableCounts()
    {
        var profile = CreateProfile(
            authUserId: Guid.NewGuid(),
            username: "owner",
            followersCount: 1,
            followingCount: 2);

        var sut = CreateSut(
            profileByAuthUserId: profile,
            followCounts: (followersCount: 14, followingCount: 7));

        var result = await sut.Service.GetMyProfileAsync(profile.AuthUserId);

        result.Status.Should().Be(ProfileQueryStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.FollowersCount.Should().Be(14);
        result.Value.FollowingCount.Should().Be(7);
    }

    [Fact]
    public async Task GetPublicProfileByUsernameAsync_ShouldUseFollowTableCounts()
    {
        var viewerProfile = CreateProfile(Guid.NewGuid(), "viewer");
        var targetProfile = CreateProfile(
            authUserId: Guid.NewGuid(),
            username: "target",
            followersCount: 1,
            followingCount: 2);

        var sut = CreateSut(
            profileByAuthUserId: viewerProfile,
            profileByUsername: targetProfile,
            followCounts: (followersCount: 31, followingCount: 9),
            followedByCurrentUser: true);

        var result = await sut.Service.GetPublicProfileByUsernameAsync(targetProfile.Username, viewerProfile.AuthUserId);

        result.Status.Should().Be(ProfileQueryStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.FollowersCount.Should().Be(31);
        result.Value.FollowingCount.Should().Be(9);
        result.Value.IsFollowedByCurrentUser.Should().BeTrue();
        sut.UserFollowRepository.Verify(
            repo => repo.ExistsActiveAsync(viewerProfile.Id, targetProfile.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPublicProfileByUsernameAsync_ShouldReturnFalseWhenFollowRowDoesNotExist()
    {
        var viewerProfile = CreateProfile(Guid.NewGuid(), "viewer");
        var targetProfile = CreateProfile(Guid.NewGuid(), "target");

        var sut = CreateSut(
            profileByAuthUserId: viewerProfile,
            profileByUsername: targetProfile);

        var result = await sut.Service.GetPublicProfileByUsernameAsync(targetProfile.Username, viewerProfile.AuthUserId);

        result.Status.Should().Be(ProfileQueryStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.IsFollowedByCurrentUser.Should().BeFalse();
    }

    [Fact]
    public async Task GetInternalUserBatchSummaryAsync_ShouldRespectFollowerVisibility()
    {
        var viewerProfile = CreateProfile(Guid.NewGuid(), "viewer");
        var targetProfile = CreateProfile(Guid.NewGuid(), "target");
        targetProfile.ProfileVisibility = ProfileVisibility.FollowersOnly;

        var sut = CreateSut(
            profileByAuthUserId: viewerProfile,
            batchProfiles: [viewerProfile, targetProfile],
            followedProfileIds: [targetProfile.Id]);

        var request = new Norge360.Accounts.Contracts.Requests.InternalUserBatchSummaryRequest([viewerProfile.AuthUserId, targetProfile.AuthUserId]);
        var result = await sut.Service.GetInternalUserBatchSummaryAsync(request, viewerProfile.AuthUserId);

        result.Items.Should().ContainSingle(item => item.UserId == targetProfile.AuthUserId && item.CanViewPosts);
    }

    private static UserProfile CreateProfile(
        Guid authUserId,
        string username,
        int followersCount = 0,
        int followingCount = 0) =>
        new()
        {
            AuthUserId = authUserId,
            Username = username,
            NormalizedUsername = username.ToLowerInvariant(),
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            ProfileVisibility = ProfileVisibility.Public,
            IsDeleted = false
        };

    private static TestContext CreateSut(
        UserProfile? profileByAuthUserId = null,
        UserProfile? profileByUsername = null,
        IReadOnlyCollection<UserProfile>? batchProfiles = null,
        (int followersCount, int followingCount)? followCounts = null,
        bool followedByCurrentUser = false,
        bool followingCurrentUser = false,
        IReadOnlyCollection<Guid>? followedProfileIds = null)
    {
        var userProfileRepository = new Mock<IUserProfileRepository>();
        userProfileRepository.Setup(repo => repo.GetByAuthUserIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByAuthUserId);
        userProfileRepository.Setup(repo => repo.GetByNormalizedUsernameAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByUsername);
        userProfileRepository.Setup(repo => repo.GetByProfileIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByUsername);
        userProfileRepository.Setup(repo => repo.ListByAuthUserIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batchProfiles ?? []);

        var userFollowRepository = new Mock<IUserFollowRepository>();
        userFollowRepository.Setup(repo => repo.CountFollowersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(followCounts?.followersCount ?? 0);
        userFollowRepository.Setup(repo => repo.CountFollowingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(followCounts?.followingCount ?? 0);
        userFollowRepository.Setup(repo => repo.ListFollowingProfileIdsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(followedProfileIds ?? []);
        var viewerProfileId = profileByAuthUserId?.Id ?? Guid.Empty;
        var targetProfileId = profileByUsername?.Id ?? Guid.Empty;
        userFollowRepository.Setup(repo => repo.ExistsActiveAsync(
                viewerProfileId,
                targetProfileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(followedByCurrentUser);
        userFollowRepository.Setup(repo => repo.ExistsActiveAsync(
                targetProfileId,
                viewerProfileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(followingCurrentUser);

        var userBlockRepository = new Mock<IUserBlockRepository>();
        userBlockRepository.Setup(repo => repo.ExistsBetweenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userBlockRepository.Setup(repo => repo.ListBlockedProfileIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        userBlockRepository.Setup(repo => repo.ListBlockerProfileIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var profileNotificationSubscriptionRepository = new Mock<IProfileNotificationSubscriptionRepository>();
        profileNotificationSubscriptionRepository.Setup(repo => repo.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var usernameNormalizer = new Mock<IUsernameNormalizer>();
        usernameNormalizer.Setup(normalizer => normalizer.Normalize(It.IsAny<string>()))
            .Returns((string value) => value.Trim().ToLowerInvariant());

        var usernameValidator = new Mock<IUsernameValidator>();
        usernameValidator.Setup(validator => validator.Validate(It.IsAny<string>()))
            .Returns(UsernameValidationResult.Valid());

        var visibilityPolicy = new Mock<IProfileVisibilityPolicy>();
        visibilityPolicy.Setup(policy => policy.Evaluate(It.IsAny<UserProfile>(), It.IsAny<Guid?>()))
            .Returns(ProfileVisibilityDecision.Full);

        return new TestContext(
            new ProfileQueryService(
                userProfileRepository.Object,
                userFollowRepository.Object,
                userBlockRepository.Object,
                profileNotificationSubscriptionRepository.Object,
                usernameNormalizer.Object,
                usernameValidator.Object,
                visibilityPolicy.Object),
            userFollowRepository);
    }

    private sealed record TestContext(
        ProfileQueryService Service,
        Mock<IUserFollowRepository> UserFollowRepository);
}
