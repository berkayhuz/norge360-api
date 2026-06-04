// <copyright file="CommunityControllerTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Norge360.Community.API.Controllers;
using Norge360.Community.API.Models;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Contracts.Requests;
using Norge360.Community.Contracts.Responses;
using Norge360.CurrentUser;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityControllerTests
{
    [Fact]
    public async Task CreatePost_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        var controller = CreateController(authenticated: false);

        var result = await controller.CreatePost(new CommunityUpsertPostFormRequest(), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreatePost_WhenCaptionAndMediaEmpty_ShouldReturnBadRequest()
    {
        var controller = CreateController(authenticated: true);

        var result = await controller.CreatePost(new CommunityUpsertPostFormRequest { Caption = "  ", MediaFiles = [] }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreatePost_WhenMoreThanEightMedia_ShouldReturnBadRequest()
    {
        var files = Enumerable.Range(0, 9).Select(i => CreateFormFile($"file-{i}.png")).ToList();
        var controller = CreateController(authenticated: true);

        var result = await controller.CreatePost(new CommunityUpsertPostFormRequest { Caption = "test", MediaFiles = files }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CommunityController CreateController(bool authenticated)
    {
        var service = new Mock<ICommunityService>();
        service.Setup(s => s.CreatePostAsync(It.IsAny<Guid>(), It.IsAny<CreateCommunityPostRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommunityPostDto(Guid.NewGuid(), Guid.NewGuid(), "x", "Oslo", "Sentrum", "Published", DateTime.UtcNow, null, 0, 0, 0, false, false, null, null, true, true, false, null, []));

        var mediaService = new Mock<ICommunityMediaService>();
        mediaService.Setup(s => s.UploadPostMediaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Norge360.Community.Application.Models.CommunityMediaUploadPayload>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var options = new DbContextOptionsBuilder<FakeCommunityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new FakeCommunityDbContext(options);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(authenticated);
        currentUser.SetupGet(x => x.UserId).Returns(authenticated ? Guid.NewGuid() : Guid.Empty);

        var cache = new Mock<IDistributedCache>();

        return new CommunityController(service.Object, mediaService.Object, db, currentUser.Object, cache.Object);
    }

    private static IFormFile CreateFormFile(string name)
    {
        var bytes = Encoding.UTF8.GetBytes("x");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "mediaFiles", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }
}

internal sealed class FakeCommunityDbContext(DbContextOptions<FakeCommunityDbContext> options)
    : DbContext(options), ICommunityDbContext
{
    public DbSet<Norge360.Community.Domain.Entities.CommunityPost> CommunityPosts => Set<Norge360.Community.Domain.Entities.CommunityPost>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityPostMedia> CommunityPostMedia => Set<Norge360.Community.Domain.Entities.CommunityPostMedia>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityComment> CommunityComments => Set<Norge360.Community.Domain.Entities.CommunityComment>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityPostLike> CommunityPostLikes => Set<Norge360.Community.Domain.Entities.CommunityPostLike>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityCommentLike> CommunityCommentLikes => Set<Norge360.Community.Domain.Entities.CommunityCommentLike>();
    public DbSet<Norge360.Community.Domain.Entities.CommunitySavedPost> CommunitySavedPosts => Set<Norge360.Community.Domain.Entities.CommunitySavedPost>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityPostReaction> CommunityPostReactions => Set<Norge360.Community.Domain.Entities.CommunityPostReaction>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityCommentReaction> CommunityCommentReactions => Set<Norge360.Community.Domain.Entities.CommunityCommentReaction>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityPostInterest> CommunityPostInterests => Set<Norge360.Community.Domain.Entities.CommunityPostInterest>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityPostReport> CommunityPostReports => Set<Norge360.Community.Domain.Entities.CommunityPostReport>();
    public DbSet<Norge360.Community.Domain.Entities.CommunityCommentReport> CommunityCommentReports => Set<Norge360.Community.Domain.Entities.CommunityCommentReport>();
}
