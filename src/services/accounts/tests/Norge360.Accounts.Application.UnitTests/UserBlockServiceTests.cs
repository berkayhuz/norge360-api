// <copyright file="UserBlockServiceTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Moq;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Application.Services;
using Norge360.Accounts.Domain.Entities;
using Xunit;

namespace Norge360.Accounts.Application.UnitTests;

public sealed class UserBlockServiceTests
{
    [Fact]
    public async Task BlockByUsernameAsync_WhenTargetExists_ShouldPersistBlock()
    {
        var blockerAuthUserId = Guid.NewGuid();
        var blockerProfile = CreateProfile(blockerAuthUserId, "owner");
        var targetProfile = CreateProfile(Guid.NewGuid(), "berkay");
        var sut = CreateSut(
            profileByAuthUserId: blockerProfile,
            profileByUsername: targetProfile);

        var result = await sut.Service.BlockByUsernameAsync(blockerAuthUserId, "berkay");

        result.Status.Should().Be(UserBlockMutationStatus.Success);
        sut.UserBlockRepository.Verify(
            repo => repo.AddAsync(
                It.Is<UserBlock>(block => block.BlockerProfileId == blockerProfile.Id && block.BlockedProfileId == targetProfile.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sut.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlockByUsernameAsync_WhenSelfBlockRequested_ShouldReturnValidationError()
    {
        var blockerAuthUserId = Guid.NewGuid();
        var blockerProfile = CreateProfile(blockerAuthUserId, "berkay");
        var sut = CreateSut(
            profileByAuthUserId: blockerProfile,
            profileByUsername: blockerProfile);

        var result = await sut.Service.BlockByUsernameAsync(blockerAuthUserId, "berkay");

        result.Status.Should().Be(UserBlockMutationStatus.ValidationFailed);
        result.ErrorCode.Should().Be("cannot_block_self");
        sut.UserBlockRepository.Verify(repo => repo.AddAsync(It.IsAny<UserBlock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnblockByUsernameAsync_WhenBlockExists_ShouldRemoveBlock()
    {
        var blockerAuthUserId = Guid.NewGuid();
        var blockerProfile = CreateProfile(blockerAuthUserId, "owner");
        var targetProfile = CreateProfile(Guid.NewGuid(), "berkay");
        var existingBlock = new UserBlock
        {
            BlockerProfileId = blockerProfile.Id,
            BlockedProfileId = targetProfile.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sut = CreateSut(
            profileByAuthUserId: blockerProfile,
            profileByUsername: targetProfile,
            existingBlock: existingBlock);

        var result = await sut.Service.UnblockByUsernameAsync(blockerAuthUserId, "berkay");

        result.Status.Should().Be(UserBlockMutationStatus.Success);
        sut.UserBlockRepository.Verify(repo => repo.Remove(existingBlock), Times.Once);
        sut.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListBlockedAsync_WhenAuthenticatedAndProvisioned_ShouldReturnPagedData()
    {
        var blockerAuthUserId = Guid.NewGuid();
        var blockerProfile = CreateProfile(blockerAuthUserId, "owner");
        var list = new[]
        {
            new UserBlockListItem(Guid.NewGuid(), "berkay", "Berkay", null, DateTimeOffset.UtcNow)
        };
        var sut = CreateSut(profileByAuthUserId: blockerProfile, listedItems: list);

        var result = await sut.Service.ListBlockedAsync(blockerAuthUserId, page: 2, pageSize: 10);

        result.Status.Should().Be(UserBlockListStatus.Success);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(1);
        sut.UserBlockRepository.Verify(
            repo => repo.ListBlockedAsync(blockerProfile.Id, 10, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockByUsernameAsync_WhenUsernameInvalid_ShouldReturnValidationFailed()
    {
        var blockerAuthUserId = Guid.NewGuid();
        var sut = CreateSut(validation: UsernameValidationResult.Invalid("username_format_invalid"));

        var result = await sut.Service.BlockByUsernameAsync(blockerAuthUserId, "x");

        result.Status.Should().Be(UserBlockMutationStatus.ValidationFailed);
        result.ErrorCode.Should().Be("username_format_invalid");
    }

    [Fact]
    public async Task ListBlockRelationsAsync_ShouldReturnBlockedAndBlockerProfileIds()
    {
        var blockerAuthUserId = Guid.NewGuid();
        var blockerProfile = CreateProfile(blockerAuthUserId, "owner");
        var blockedProfileId = Guid.NewGuid();
        var blockerProfileId = Guid.NewGuid();
        var sut = CreateSut(profileByAuthUserId: blockerProfile);
        sut.UserBlockRepository.Setup(repo => repo.ListBlockedProfileIdsAsync(blockerProfile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([blockedProfileId]);
        sut.UserBlockRepository.Setup(repo => repo.ListBlockerProfileIdsAsync(blockerProfile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([blockerProfileId]);
        sut.UserProfileRepository.Setup(repo => repo.ListExistingProfileIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => ids);

        var result = await sut.Service.ListBlockRelationsAsync(blockerAuthUserId);

        result.Status.Should().Be(UserBlockListStatus.Success);
        result.BlockedProfileIds.Should().ContainSingle().Which.Should().Be(blockedProfileId);
        result.BlockerProfileIds.Should().ContainSingle().Which.Should().Be(blockerProfileId);
    }

    private static UserProfile CreateProfile(Guid authUserId, string username) =>
        new()
        {
            AuthUserId = authUserId,
            Username = username,
            NormalizedUsername = username.ToLowerInvariant()
        };

    private static TestContext CreateSut(
        UserProfile? profileByAuthUserId = null,
        UserProfile? profileByUsername = null,
        UserBlock? existingBlock = null,
        IReadOnlyCollection<UserBlockListItem>? listedItems = null,
        UsernameValidationResult? validation = null)
    {
        var unitOfWork = new Mock<IAccountsUnitOfWork>();
        unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var userBlockRepository = new Mock<IUserBlockRepository>();
        userBlockRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlock);
        userBlockRepository.Setup(repo => repo.ListBlockedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listedItems ?? []);

        var userFollowRepository = new Mock<IUserFollowRepository>();

        var userProfileRepository = new Mock<IUserProfileRepository>();
        userProfileRepository.Setup(repo => repo.GetByAuthUserIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByAuthUserId);
        userProfileRepository.Setup(repo => repo.GetTrackedByAuthUserIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByAuthUserId);
        userProfileRepository.Setup(repo => repo.GetByNormalizedUsernameAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByUsername);
        userProfileRepository.Setup(repo => repo.GetTrackedByNormalizedUsernameAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileByUsername);

        var usernameNormalizer = new Mock<IUsernameNormalizer>();
        usernameNormalizer.Setup(normalizer => normalizer.Normalize(It.IsAny<string?>()))
            .Returns((string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty);

        var usernameValidator = new Mock<IUsernameValidator>();
        usernameValidator.Setup(validator => validator.Validate(It.IsAny<string?>()))
            .Returns(validation ?? UsernameValidationResult.Valid());

        return new TestContext(
            new UserBlockService(
                unitOfWork.Object,
                userBlockRepository.Object,
                userFollowRepository.Object,
                userProfileRepository.Object,
                usernameNormalizer.Object,
                usernameValidator.Object),
            unitOfWork,
            userBlockRepository,
            userProfileRepository);
    }

    private sealed record TestContext(
        UserBlockService Service,
        Mock<IAccountsUnitOfWork> UnitOfWork,
        Mock<IUserBlockRepository> UserBlockRepository,
        Mock<IUserProfileRepository> UserProfileRepository);
}
