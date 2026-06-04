using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Norge360.Community.API.Controllers;
using Norge360.Community.API.Models;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;
using Norge360.Community.Contracts.Requests;
using Norge360.Community.Contracts.Responses;
using Norge360.Community.Domain.Entities;
using Norge360.Community.Domain.Enums;
using Norge360.CurrentUser;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityControllerConsistencyTests
{
    [Fact]
    public async Task CreatePost_ShouldRollbackCreatedPostWhenMediaUploadFails()
    {
        using var fixture = new CommunityTestFixture();
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var service = new Mock<ICommunityService>();
        service.Setup(x => x.CreatePostAsync(userId, It.IsAny<CreateCommunityPostRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePostDto(postId, userId));
        service.Setup(x => x.DeletePostAsync(postId, userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var media = new Mock<ICommunityMediaService>();
        media.Setup(x => x.UploadPostMediaAsync(postId, userId, It.IsAny<IReadOnlyList<CommunityMediaUploadPayload>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("community_media_invalid_type"));
        var controller = CreateController(fixture, service, media, userId);

        var result = await controller.CreatePost(new CommunityUpsertPostFormRequest { MediaFiles = [CreateFormFile()] }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        service.Verify(x => x.DeletePostAsync(postId, userId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_ShouldRejectNinthMediaBeforeUpdatingCaption()
    {
        using var fixture = new CommunityTestFixture();
        var userId = Guid.NewGuid();
        var post = fixture.AddPost(userId);
        var existingMedia = Enumerable.Range(0, 8).Select(index => new CommunityPostMedia
        {
            PostId = post.Id,
            StorageKey = $"key-{index}",
            PublicUrl = $"https://cdn.test/{index}",
            ContentType = "image/png",
            SizeBytes = 1,
            Width = 1,
            Height = 1,
            Order = (short)index,
            Status = CommunityMediaStatus.Ready
        }).ToList();
        fixture.Db.CommunityPostMedia.AddRange(existingMedia);
        await fixture.Db.SaveChangesAsync();
        var service = new Mock<ICommunityService>();
        var controller = CreateController(fixture, service, new Mock<ICommunityMediaService>(), userId);

        var result = await controller.UpdatePost(post.Id, new CommunityUpsertPostFormRequest
        {
            Caption = "updated",
            ExistingMediaIds = existingMedia.Select(x => x.Id).ToList(),
            MediaOrder = existingMedia.Select(x => x.Id).ToList(),
            MediaFiles = [CreateFormFile()]
        }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        service.Verify(x => x.UpdatePostAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<UpdateCommunityPostRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModerationHideAndRestore_ShouldChangeFeedAndCommentVisibility()
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();
        var comment = fixture.AddComment(post);
        var controller = CreateController(fixture, new Mock<ICommunityService>(), new Mock<ICommunityMediaService>(), Guid.NewGuid());

        await controller.HidePost(post.Id, CancellationToken.None);
        fixture.Db.CommunityPosts.Single().Status.Should().Be(CommunityPostStatus.Hidden);
        await controller.RestorePost(post.Id, CancellationToken.None);
        fixture.Db.CommunityPosts.Single().Status.Should().Be(CommunityPostStatus.Published);
        await controller.HideComment(comment.Id, CancellationToken.None);
        (await fixture.Db.CommunityComments.CountAsync()).Should().Be(0);
        await controller.RestoreComment(comment.Id, CancellationToken.None);
        (await fixture.Db.CommunityComments.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(nameof(CommunityController.GetModerationReports))]
    [InlineData(nameof(CommunityController.HidePost))]
    [InlineData(nameof(CommunityController.RestorePost))]
    [InlineData(nameof(CommunityController.HideComment))]
    [InlineData(nameof(CommunityController.RestoreComment))]
    public void ModerationEndpoints_ShouldRequireAdminOrModeratorRole(string methodName)
    {
        var method = typeof(CommunityController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Contain("Admin").And.Contain("Moderator");
    }

    private static CommunityController CreateController(
        CommunityTestFixture fixture,
        Mock<ICommunityService> service,
        Mock<ICommunityMediaService> media,
        Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(userId);
        var cache = new Mock<IDistributedCache>();
        return new CommunityController(service.Object, media.Object, fixture.Db, currentUser.Object, cache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static CommunityPostDto CreatePostDto(Guid postId, Guid userId) =>
        new(postId, userId, null, "Oslo", "Sentrum", "Published", DateTime.UtcNow, null, 0, 0, 0, false, false, null, null, true, true, false, null, []);

    private static IFormFile CreateFormFile()
    {
        var bytes = Encoding.UTF8.GetBytes("image");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "mediaFiles", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }
}
