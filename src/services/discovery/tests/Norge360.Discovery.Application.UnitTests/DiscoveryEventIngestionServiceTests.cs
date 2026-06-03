using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Norge360.Discovery.API.Controllers;
using Norge360.Discovery.API.Security;
using Norge360.Discovery.API.Services;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Application.Services;
using Norge360.Discovery.Contracts.Requests;
using Norge360.Discovery.Contracts.Responses;
using Norge360.Discovery.Domain.Entities;
using Norge360.Discovery.Domain.Enums;
using Norge360.Discovery.Infrastructure.Persistence;
using System.Text.Json;
using Xunit;

namespace Norge360.Discovery.Application.UnitTests;

public sealed class DiscoveryEventIngestionServiceTests
{
    [Fact]
    public async Task FollowEvent_ShouldCreateAggregatePoints()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var targetUserId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("ProfileFollowed", "follow-1", actorUserId: Guid.NewGuid(), targetUserId: targetUserId));

        var aggregate = await dbContext.DiscoveryDailyAggregates.SingleAsync();
        aggregate.TargetType.Should().Be(DiscoverySubjectType.User);
        aggregate.TargetId.Should().Be(targetUserId);
        aggregate.FollowPoints.Should().Be(3);
        aggregate.RawScore.Should().Be(3);
    }

    [Fact]
    public async Task Unfollow_ShouldRemoveFollowImpact()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("ProfileFollowed", "follow-1", actorUserId, targetUserId, sourceEntityId: "follow-source"));
        await service.IngestAsync(CreateEvent("ProfileUnfollowed", "unfollow-1", actorUserId, targetUserId, sourceEntityId: "follow-source"));

        var aggregate = await dbContext.DiscoveryDailyAggregates.SingleAsync();
        aggregate.FollowPoints.Should().Be(0);
        aggregate.RawScore.Should().Be(0);
    }

    [Fact]
    public async Task ProfileView_ShouldScoreOnlyOncePerActorTargetDate()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("ProfileViewed", "view-1", actorUserId, targetUserId));
        await service.IngestAsync(CreateEvent("ProfileViewed", "view-2", actorUserId, targetUserId));

        var aggregate = await dbContext.DiscoveryDailyAggregates.SingleAsync();
        aggregate.ProfileViewPoints.Should().Be(1);
        aggregate.RawScore.Should().Be(1);
        (await dbContext.DiscoveryEvents.CountAsync(x => x.IsValid)).Should().Be(1);
    }

    [Fact]
    public async Task DuplicateEvent_ShouldNotScoreTwice()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var request = CreateEvent("PostLiked", "like-1", Guid.NewGuid(), Guid.NewGuid());

        await service.IngestAsync(request);
        var response = await service.IngestAsync(request);

        response.Duplicates.Should().Be(1);
        var aggregate = await dbContext.DiscoveryDailyAggregates.SingleAsync();
        aggregate.LikePoints.Should().Be(1);
    }

    [Fact]
    public async Task SelfInteraction_ShouldNotCreatePoints()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var userId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("PostLiked", "self-like", userId, userId));

        (await dbContext.DiscoveryDailyAggregates.CountAsync()).Should().Be(0);
        var discoveryEvent = await dbContext.DiscoveryEvents.SingleAsync();
        discoveryEvent.IsValid.Should().BeFalse();
        discoveryEvent.InvalidReason.Should().Be("self_interaction");
    }

    [Fact]
    public async Task SelfProfileView_ShouldNotCreatePoints()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var userId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("ProfileViewed", "self-view", userId, userId));

        (await dbContext.DiscoveryDailyAggregates.CountAsync()).Should().Be(0);
        var discoveryEvent = await dbContext.DiscoveryEvents.SingleAsync();
        discoveryEvent.IsValid.Should().BeFalse();
        discoveryEvent.InvalidReason.Should().Be("self_interaction");
    }

    [Fact]
    public async Task SelfComment_ShouldNotCreatePoints()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var userId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("PostCommented", "self-comment", userId, userId, metadataJson: JsonSerializer.Serialize(new { body = "valid comment" })));

        (await dbContext.DiscoveryDailyAggregates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Unlike_ShouldRemoveLikeImpact()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("PostLiked", "like-1", actorUserId, targetUserId, sourceEntityId: "post-1"));
        await service.IngestAsync(CreateEvent("PostUnliked", "unlike-1", actorUserId, targetUserId, sourceEntityId: "post-1"));

        var aggregate = await dbContext.DiscoveryDailyAggregates.SingleAsync();
        aggregate.LikePoints.Should().Be(0);
        aggregate.RawScore.Should().Be(0);
    }

    [Fact]
    public async Task CommentDeleted_ShouldRemoveCommentImpact()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        await service.IngestAsync(CreateEvent("PostCommented", "comment-1", actorUserId, targetUserId, sourceEntityId: "comment-source", metadataJson: JsonSerializer.Serialize(new { body = "valid comment" })));
        await service.IngestAsync(CreateEvent("PostCommentDeleted", "comment-delete-1", actorUserId, targetUserId, sourceEntityId: "comment-source"));

        var aggregate = await dbContext.DiscoveryDailyAggregates.SingleAsync();
        aggregate.CommentPoints.Should().Be(0);
        aggregate.RawScore.Should().Be(0);
    }

    [Fact]
    public async Task InvalidComment_ShouldNotCreatePoints()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryEventIngestionService(dbContext);

        await service.IngestAsync(CreateEvent("PostCommented", "emoji-comment", Guid.NewGuid(), Guid.NewGuid(), metadataJson: JsonSerializer.Serialize(new { body = "..." })));

        (await dbContext.DiscoveryDailyAggregates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PrivateDeletedInactiveProfiles_ShouldNotAppearInDiscoverResponse()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryRankingService(dbContext);
        var publicProfileId = Guid.NewGuid();
        var privateProfileId = Guid.NewGuid();
        var deletedProfileId = Guid.NewGuid();
        var inactiveProfileId = Guid.NewGuid();

        AddRanking(dbContext, publicProfileId, 1);
        AddRanking(dbContext, privateProfileId, 2);
        AddRanking(dbContext, deletedProfileId, 3);
        AddRanking(dbContext, inactiveProfileId, 4);
        AddSnapshot(dbContext, publicProfileId, "public-user", "Public", isActive: true, isDeleted: false);
        AddSnapshot(dbContext, privateProfileId, "private-user", "Private", isActive: true, isDeleted: false);
        AddSnapshot(dbContext, deletedProfileId, "deleted-user", "Public", isActive: true, isDeleted: true);
        AddSnapshot(dbContext, inactiveProfileId, "inactive-user", "Public", isActive: false, isDeleted: false);
        await dbContext.SaveChangesAsync();

        var response = await service.GetPopularUsersAsync(10, null);

        response.Should().ContainSingle();
        response[0].ProfileId.Should().Be(publicProfileId);
    }

    [Fact]
    public async Task SnapshotBatchUpsert_ShouldCreateSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoverySnapshotService(dbContext);
        var profileId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();

        var response = await service.UpsertBatchAsync(new DiscoverySnapshotBatchRequest([
            CreateSnapshot(profileId, authUserId, "new-user", followersCount: 4, postsCount: 2)
        ]));

        response.Accepted.Should().Be(1);
        response.Created.Should().Be(1);
        var snapshot = await dbContext.DiscoverySubjectSnapshots.SingleAsync();
        snapshot.SubjectId.Should().Be(profileId);
        snapshot.AuthUserId.Should().Be(authUserId);
        snapshot.Username.Should().Be("new-user");
        snapshot.FollowersCount.Should().Be(4);
        snapshot.PostsCount.Should().Be(2);
    }

    [Fact]
    public async Task SnapshotBatchUpsert_ShouldUpdateExistingSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoverySnapshotService(dbContext);
        var profileId = Guid.NewGuid();

        await service.UpsertBatchAsync(new DiscoverySnapshotBatchRequest([
            CreateSnapshot(profileId, Guid.NewGuid(), "old-user", followersCount: 1, postsCount: 1)
        ]));

        var response = await service.UpsertBatchAsync(new DiscoverySnapshotBatchRequest([
            CreateSnapshot(profileId, Guid.NewGuid(), "new-user", displayName: "New User", followersCount: 7, postsCount: 5)
        ]));

        response.Updated.Should().Be(1);
        (await dbContext.DiscoverySubjectSnapshots.CountAsync()).Should().Be(1);
        var snapshot = await dbContext.DiscoverySubjectSnapshots.SingleAsync();
        snapshot.Username.Should().Be("new-user");
        snapshot.DisplayName.Should().Be("New User");
        snapshot.FollowersCount.Should().Be(7);
        snapshot.PostsCount.Should().Be(5);
    }

    [Fact]
    public async Task SnapshotBatchUpsert_ShouldBeIdempotentForDuplicateRuns()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoverySnapshotService(dbContext);
        var profileId = Guid.NewGuid();
        var request = new DiscoverySnapshotBatchRequest([
            CreateSnapshot(profileId, Guid.NewGuid(), "stable-user", followersCount: 3, postsCount: 9)
        ]);

        await service.UpsertBatchAsync(request);
        await service.UpsertBatchAsync(request);

        (await dbContext.DiscoverySubjectSnapshots.CountAsync()).Should().Be(1);
        var snapshot = await dbContext.DiscoverySubjectSnapshots.SingleAsync();
        snapshot.Username.Should().Be("stable-user");
        snapshot.FollowersCount.Should().Be(3);
        snapshot.PostsCount.Should().Be(9);
    }

    [Fact]
    public async Task RankingEmptyButSnapshotsAvailable_ShouldUseSnapshotFallback()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryRankingService(dbContext);
        var firstProfileId = Guid.NewGuid();
        var secondProfileId = Guid.NewGuid();

        AddSnapshot(dbContext, firstProfileId, "first-user", "Public", isActive: true, isDeleted: false, followersCount: 8, postsCount: 1);
        AddSnapshot(dbContext, secondProfileId, "second-user", "Public", isActive: true, isDeleted: false, followersCount: 2, postsCount: 9);
        await dbContext.SaveChangesAsync();

        var response = await service.GetPopularUsersAsync(10, null);

        response.Should().HaveCount(2);
        response[0].ProfileId.Should().Be(firstProfileId);
        response[1].ProfileId.Should().Be(secondProfileId);
    }

    [Fact]
    public async Task RankingAvailable_ShouldUseRankingBeforeSnapshotFallback()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryRankingService(dbContext);
        var highFollowerProfileId = Guid.NewGuid();
        var rankedFirstProfileId = Guid.NewGuid();

        AddSnapshot(dbContext, highFollowerProfileId, "high-followers", "Public", isActive: true, isDeleted: false, followersCount: 99, postsCount: 99);
        AddSnapshot(dbContext, rankedFirstProfileId, "ranked-first", "Public", isActive: true, isDeleted: false, followersCount: 1, postsCount: 1);
        AddRanking(dbContext, rankedFirstProfileId, 1);
        AddRanking(dbContext, highFollowerProfileId, 2);
        await dbContext.SaveChangesAsync();

        var response = await service.GetPopularUsersAsync(10, null);

        response.Should().HaveCount(2);
        response[0].ProfileId.Should().Be(rankedFirstProfileId);
    }

    [Fact]
    public async Task SnapshotFallback_ShouldExcludePrivateDeletedInactiveProfiles()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryRankingService(dbContext);
        var publicProfileId = Guid.NewGuid();

        AddSnapshot(dbContext, publicProfileId, "public-user", "Public", isActive: true, isDeleted: false, followersCount: 1, postsCount: 1);
        AddSnapshot(dbContext, Guid.NewGuid(), "private-user", "Private", isActive: true, isDeleted: false, followersCount: 9, postsCount: 9);
        AddSnapshot(dbContext, Guid.NewGuid(), "deleted-user", "Public", isActive: true, isDeleted: true, followersCount: 9, postsCount: 9);
        AddSnapshot(dbContext, Guid.NewGuid(), "inactive-user", "Public", isActive: false, isDeleted: false, followersCount: 9, postsCount: 9);
        await dbContext.SaveChangesAsync();

        var response = await service.GetPopularUsersAsync(10, null);

        response.Should().ContainSingle();
        response[0].ProfileId.Should().Be(publicProfileId);
    }

    [Fact]
    public void DiscoverUserResponse_ShouldNotSerializeScore()
    {
        var response = new DiscoverUserResponse(Guid.NewGuid(), Guid.NewGuid(), "user", "User", null, null, false, false, "reason");

        var json = JsonSerializer.Serialize(response);

        var lowerJson = json.ToLowerInvariant();
        lowerJson.Should().NotContain("score");
        lowerJson.Should().NotContain("rawscore");
        lowerJson.Should().NotContain("finalscore");
    }

    [Fact]
    public async Task RankingService_ShouldFillRankingTableInExpectedOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = new DiscoveryRankingService(dbContext);
        var firstProfileId = Guid.NewGuid();
        var secondProfileId = Guid.NewGuid();

        AddAggregate(dbContext, firstProfileId, 10);
        AddAggregate(dbContext, secondProfileId, 3);
        await dbContext.SaveChangesAsync();

        await service.RecomputeAsync();

        var rankings = await dbContext.DiscoveryRankings
            .Where(x => x.RankingType == DiscoveryRankingType.PopularUsers)
            .OrderBy(x => x.Rank)
            .ToListAsync();
        rankings.Should().HaveCount(2);
        rankings[0].TargetId.Should().Be(firstProfileId);
        rankings[1].TargetId.Should().Be(secondProfileId);
    }

    [Fact]
    public async Task InternalSnapshotEndpoint_ShouldReturnForbiddenWithoutToken()
    {
        var controller = CreateInternalMaintenanceController(new DiscoveryInternalEventOptions
        {
            Enabled = true,
            HeaderName = "X-Discovery-Internal-Token",
            Token = "secret"
        });

        var result = await controller.UpsertSnapshots(new DiscoverySnapshotBatchRequest([]), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InternalSnapshotEndpoint_ShouldAcceptValidToken()
    {
        var controller = CreateInternalMaintenanceController(new DiscoveryInternalEventOptions
        {
            Enabled = true,
            HeaderName = "X-Discovery-Internal-Token",
            Token = "secret"
        });
        controller.ControllerContext.HttpContext.Request.Headers["X-Discovery-Internal-Token"] = "secret";

        var result = await controller.UpsertSnapshots(new DiscoverySnapshotBatchRequest([]), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<DiscoverySnapshotBatchUpsertResponse>();
    }

    private static DiscoveryEventRequest CreateEvent(
        string eventType,
        string key,
        Guid? actorUserId,
        Guid? targetUserId,
        string sourceEntityId = "source-1",
        string? metadataJson = null) =>
        new(
            eventType,
            "UnitTest",
            "Entity",
            sourceEntityId,
            actorUserId,
            null,
            targetUserId,
            targetUserId,
            "User",
            targetUserId?.ToString("D"),
            key,
            DateTime.UtcNow,
            metadataJson);

    private static void AddAggregate(DiscoveryDbContext dbContext, Guid profileId, int rawScore) =>
        dbContext.DiscoveryDailyAggregates.Add(new DiscoveryDailyAggregate
        {
            TargetType = DiscoverySubjectType.User,
            TargetId = profileId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            RawScore = rawScore,
            FollowPoints = rawScore,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    private static void AddRanking(DiscoveryDbContext dbContext, Guid profileId, int rank) =>
        dbContext.DiscoveryRankings.Add(new DiscoveryRanking
        {
            RankingType = DiscoveryRankingType.PopularUsers,
            TargetType = DiscoverySubjectType.User,
            TargetId = profileId,
            Score = 100 - rank,
            Rank = rank,
            WindowStart = DateTime.UtcNow.AddDays(-7),
            WindowEnd = DateTime.UtcNow,
            ComputedAt = DateTime.UtcNow
        });

    private static void AddSnapshot(
        DiscoveryDbContext dbContext,
        Guid profileId,
        string username,
        string visibility,
        bool isActive,
        bool isDeleted,
        int followersCount = 0,
        int postsCount = 0) =>
        dbContext.DiscoverySubjectSnapshots.Add(new DiscoverySubjectSnapshot
        {
            SubjectType = DiscoverySubjectType.User,
            SubjectId = profileId,
            AuthUserId = Guid.NewGuid(),
            Username = username,
            DisplayName = username,
            Visibility = visibility,
            IsActive = isActive,
            IsDeleted = isDeleted,
            FollowersCount = followersCount,
            PostsCount = postsCount,
            UpdatedAt = DateTime.UtcNow
        });

    private static DiscoverySnapshotUpsertRequest CreateSnapshot(
        Guid profileId,
        Guid authUserId,
        string username,
        string? displayName = null,
        int followersCount = 0,
        int postsCount = 0) =>
        new(
            profileId,
            authUserId,
            username,
            displayName,
            null,
            "bio",
            false,
            "Public",
            true,
            false,
            followersCount,
            postsCount,
            DateTimeOffset.UtcNow);

    private static DiscoveryInternalMaintenanceController CreateInternalMaintenanceController(DiscoveryInternalEventOptions options)
    {
        var controller = new DiscoveryInternalMaintenanceController(
            new FakeSnapshotService(),
            new FakeRankingService(),
            new FakeAccountsBackfillService(),
            Options.Create(options),
            NullLogger<DiscoveryInternalMaintenanceController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private sealed class FakeSnapshotService : IDiscoverySnapshotService
    {
        public Task<DiscoverySnapshotBatchUpsertResponse> UpsertBatchAsync(
            DiscoverySnapshotBatchRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DiscoverySnapshotBatchUpsertResponse(request.Snapshots.Count, 0, request.Snapshots.Count, 0));
    }

    private sealed class FakeRankingService : IDiscoveryRankingService
    {
        public Task RecomputeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<DiscoverUserResponse>> GetPopularUsersAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoverUserResponse>>([]);

        public Task<IReadOnlyList<DiscoverUserResponse>> GetTrendingUsersAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoverUserResponse>>([]);

        public Task<IReadOnlyList<DiscoverUserResponse>> GetFollowSuggestionsAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoverUserResponse>>([]);

        public Task<DiscoveryHubResponse> GetHubAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DiscoveryHubResponse([], [], []));
    }

    private sealed class FakeAccountsBackfillService : IAccountsDiscoveryBackfillService
    {
        public Task<DiscoveryBackfillResponse> BackfillAsync(int take, int maxBatches, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DiscoveryBackfillResponse(0, 0, 0, 0, 0));
    }

    private static DiscoveryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiscoveryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new DiscoveryDbContext(options);
    }
}
