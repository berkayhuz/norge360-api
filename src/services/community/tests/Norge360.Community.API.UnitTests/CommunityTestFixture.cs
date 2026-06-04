// <copyright file="CommunityTestFixture.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Moq;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;
using Norge360.Community.Application.Services;
using Norge360.Community.Domain.Entities;
using Norge360.Community.Domain.Enums;
using Norge360.Community.Infrastructure.Persistence;

namespace Norge360.Community.API.UnitTests;

internal sealed class CommunityTestFixture : IDisposable
{
    public CommunityTestFixture()
    {
        var options = new DbContextOptionsBuilder<CommunityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new CommunityDbContext(options);

        Authors.Setup(x => x.GetAuthorSummariesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, Guid? _, CancellationToken _) =>
                ids.Distinct().ToDictionary(id => id, id => new CommunityAuthorSummary(id, $"user-{id:N}", "Test User", null, false, true)));
        Visibility.Setup(x => x.FilterVisibleAuthorsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, Guid? _, CancellationToken _) => ids.ToHashSet());
        Visibility.Setup(x => x.CanViewAuthorPostsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Publisher.Setup(x => x.PublishAsync(It.IsAny<DiscoveryEventEnvelope>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Service = new CommunityService(Db, Authors.Object, Visibility.Object, Publisher.Object);
    }

    public CommunityDbContext Db { get; }
    public Mock<ICommunityAuthorProfileProvider> Authors { get; } = new();
    public Mock<ICommunityVisibilityService> Visibility { get; } = new();
    public Mock<IDiscoveryEventPublisher> Publisher { get; } = new();
    public CommunityService Service { get; }

    public CommunityPost AddPost(Guid? userId = null, CommunityPostStatus status = CommunityPostStatus.Published, string? caption = "hello")
    {
        var post = new CommunityPost { UserId = userId ?? Guid.NewGuid(), Caption = caption, Status = status, CreatedAt = DateTime.UtcNow };
        Db.CommunityPosts.Add(post);
        Db.SaveChanges();
        return post;
    }

    public CommunityComment AddComment(CommunityPost post, Guid? userId = null, string body = "comment")
    {
        var comment = new CommunityComment { PostId = post.Id, UserId = userId ?? Guid.NewGuid(), Body = body, CreatedAt = DateTime.UtcNow };
        Db.CommunityComments.Add(comment);
        Db.SaveChanges();
        return comment;
    }

    public void Dispose() => Db.Dispose();
}
