using FluentAssertions;
using Norge360.Community.Contracts.Requests;
using Norge360.Community.Domain.Enums;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityServiceCommentTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddComment_ShouldRejectBlankBody(string body)
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();

        var action = () => fixture.Service.AddCommentAsync(post.Id, Guid.NewGuid(), new CreateCommunityCommentRequest(body), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("comment_body_required");
    }

    [Fact]
    public async Task AddComment_ShouldAcceptMaximumLengthAndRejectAboveMaximum()
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();

        var accepted = await fixture.Service.AddCommentAsync(post.Id, Guid.NewGuid(), new CreateCommunityCommentRequest(new string('a', 1000)), CancellationToken.None);
        var rejected = () => fixture.Service.AddCommentAsync(post.Id, Guid.NewGuid(), new CreateCommunityCommentRequest(new string('a', 1001)), CancellationToken.None);

        accepted.Should().NotBeNull();
        await rejected.Should().ThrowAsync<ArgumentException>().WithMessage("comment_body_max_length");
    }

    [Theory]
    [InlineData(CommunityPostStatus.Hidden)]
    [InlineData(CommunityPostStatus.Removed)]
    public async Task AddCommentAndReply_ShouldRejectInactivePost(CommunityPostStatus status)
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost(status: status);
        post.Status = CommunityPostStatus.Published;
        var comment = fixture.AddComment(post);
        post.Status = status;
        await fixture.Db.SaveChangesAsync();

        var added = await fixture.Service.AddCommentAsync(post.Id, Guid.NewGuid(), new CreateCommunityCommentRequest("new"), CancellationToken.None);
        var reply = await fixture.Service.ReplyCommentAsync(comment.Id, Guid.NewGuid(), new CreateCommunityReplyRequest("reply"), CancellationToken.None);

        added.Should().BeNull();
        reply.Should().BeNull();
    }

    [Fact]
    public async Task ReplyAndDelete_ShouldRespectParentAndOwnership()
    {
        using var fixture = new CommunityTestFixture();
        var owner = Guid.NewGuid();
        var post = fixture.AddPost();
        var comment = fixture.AddComment(post, owner);

        var reply = await fixture.Service.ReplyCommentAsync(comment.Id, Guid.NewGuid(), new CreateCommunityReplyRequest("reply"), CancellationToken.None);
        var nonOwnerDelete = () => fixture.Service.DeleteCommentAsync(comment.Id, Guid.NewGuid(), false, CancellationToken.None);

        reply!.ParentCommentId.Should().Be(comment.Id);
        await nonOwnerDelete.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("comment_delete_forbidden");
        (await fixture.Service.DeleteCommentAsync(comment.Id, Guid.NewGuid(), true, CancellationToken.None)).Should().BeTrue();
    }
}
