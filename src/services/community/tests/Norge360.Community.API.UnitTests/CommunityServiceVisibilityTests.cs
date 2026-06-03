using FluentAssertions;
using Moq;
using Norge360.Community.Application.Abstractions;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityServiceVisibilityTests
{
    [Fact]
    public async Task Feed_ShouldFilterInvisibleAuthorsAndAvoidPerPostVisibilityCalls()
    {
        using var fixture = new CommunityTestFixture();
        var visibleAuthorId = Guid.NewGuid();
        var hiddenAuthorId = Guid.NewGuid();
        fixture.AddPost(visibleAuthorId);
        fixture.AddPost(visibleAuthorId);
        fixture.AddPost(hiddenAuthorId);
        fixture.Visibility.Setup(x => x.FilterVisibleAuthorsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { visibleAuthorId });

        var feed = await fixture.Service.GetFeedAsync(1, 20, Guid.NewGuid(), CancellationToken.None);

        feed.Items.Should().HaveCount(2).And.OnlyContain(x => x.Post.UserId == visibleAuthorId);
        fixture.Visibility.Verify(x => x.FilterVisibleAuthorsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Visibility.Verify(x => x.CanViewAuthorPostsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Authors.Verify(x => x.GetAuthorSummariesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostDetailsAndUserPosts_ShouldRespectVisibilityDecision()
    {
        using var fixture = new CommunityTestFixture();
        var authorId = Guid.NewGuid();
        var post = fixture.AddPost(authorId);
        fixture.Visibility.Setup(x => x.CanViewAuthorPostsAsync(authorId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var details = await fixture.Service.GetPostAsync(post.Id, Guid.NewGuid(), CancellationToken.None);
        var profileFeed = await fixture.Service.GetUserPostsAsync(authorId, 1, 20, Guid.NewGuid(), CancellationToken.None);

        details.Should().BeNull();
        profileFeed.Items.Should().BeEmpty();
    }
}
