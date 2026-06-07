// <copyright file="CommunityServiceInteractionTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Norge360.Community.Application.Models;
using Norge360.Community.Contracts.Requests;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityServiceInteractionTests
{
    [Fact]
    public async Task CreatePostAsync_ShouldPublishPostCreatedNotificationWithFirstPostFlag()
    {
        using var fixture = new CommunityTestFixture();
        var userId = Guid.NewGuid();

        var post = await fixture.Service.CreatePostAsync(userId, new CreateCommunityPostRequest("hello", "Oslo", null), CancellationToken.None);

        fixture.Notifications.Verify(
            x => x.PublishPostCreatedAsync(
                It.Is<CommunityPostPublishedNotification>(n =>
                    n.PostId == post.Id &&
                    n.Author.UserId == userId &&
                    n.City == "Oslo" &&
                    n.IsFirstPost),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostLikeAndCommentLike_ShouldPublishNotificationsOnlyWhenNewLikeIsFromAnotherUser()
    {
        using var fixture = new CommunityTestFixture();
        var ownerId = Guid.NewGuid();
        var likerId = Guid.NewGuid();
        var post = fixture.AddPost(ownerId);
        var comment = fixture.AddComment(post, ownerId);

        await fixture.Service.SetPostLikeAsync(post.Id, likerId, true, CancellationToken.None);
        await fixture.Service.SetPostLikeAsync(post.Id, likerId, true, CancellationToken.None);
        await fixture.Service.SetCommentLikeAsync(comment.Id, likerId, true, CancellationToken.None);
        await fixture.Service.SetCommentLikeAsync(comment.Id, likerId, true, CancellationToken.None);

        fixture.Notifications.Verify(
            x => x.PublishPostLikedAsync(
                It.Is<CommunityInteractionNotification>(n => n.RecipientUserId == ownerId && n.PostId == post.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Notifications.Verify(
            x => x.PublishCommentLikedAsync(
                It.Is<CommunityInteractionNotification>(n => n.RecipientUserId == ownerId && n.PostId == post.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommentsAndReplies_ShouldNotifyPostOwnerAndParentCommentOwner()
    {
        using var fixture = new CommunityTestFixture();
        var postOwnerId = Guid.NewGuid();
        var commenterId = Guid.NewGuid();
        var replierId = Guid.NewGuid();
        var post = fixture.AddPost(postOwnerId);
        var comment = await fixture.Service.AddCommentAsync(
            post.Id,
            commenterId,
            new CreateCommunityCommentRequest("new comment"),
            CancellationToken.None);

        await fixture.Service.ReplyCommentAsync(
            comment!.Id,
            replierId,
            new CreateCommunityReplyRequest("reply"),
            CancellationToken.None);

        fixture.Notifications.Verify(
            x => x.PublishPostCommentedAsync(
                It.Is<CommunityInteractionNotification>(n => n.RecipientUserId == postOwnerId && n.EntityId == comment.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Notifications.Verify(
            x => x.PublishCommentRepliedAsync(
                It.Is<CommunityInteractionNotification>(n => n.RecipientUserId == commenterId && n.PostId == post.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LikeAndSaveToggles_ShouldNotCreateDuplicatesAndShouldReturnCounts()
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();
        var comment = fixture.AddComment(post);
        var userId = Guid.NewGuid();

        (await fixture.Service.SetPostLikeAsync(post.Id, userId, true, CancellationToken.None))!.Count.Should().Be(1);
        (await fixture.Service.SetPostLikeAsync(post.Id, userId, true, CancellationToken.None))!.Count.Should().Be(1);
        (await fixture.Service.SetPostLikeAsync(post.Id, userId, false, CancellationToken.None))!.Count.Should().Be(0);
        (await fixture.Service.SetSavedPostAsync(post.Id, userId, true, CancellationToken.None))!.Count.Should().Be(1);
        (await fixture.Service.SetSavedPostAsync(post.Id, userId, false, CancellationToken.None))!.Count.Should().Be(0);
        (await fixture.Service.SetCommentLikeAsync(comment.Id, userId, true, CancellationToken.None))!.Count.Should().Be(1);
        (await fixture.Service.SetCommentLikeAsync(comment.Id, userId, false, CancellationToken.None))!.Count.Should().Be(0);
    }

    [Fact]
    public async Task NotInterestedAndClearInterest_ShouldHideAndRestorePostInFeed()
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();
        var userId = Guid.NewGuid();

        await fixture.Service.SetPostInterestAsync(post.Id, userId, new SetCommunityPostInterestRequest("NotInterested"), CancellationToken.None);
        var hiddenFeed = await fixture.Service.GetFeedAsync(1, 20, userId, CancellationToken.None);
        await fixture.Service.ClearPostInterestAsync(post.Id, userId, CancellationToken.None);
        var visibleFeed = await fixture.Service.GetFeedAsync(1, 20, userId, CancellationToken.None);

        hiddenFeed.Items.Should().BeEmpty();
        visibleFeed.Items.Should().ContainSingle(x => x.Post.Id == post.Id);
    }

    [Fact]
    public async Task Reactions_ShouldAddUpdateRemoveAndKeepSingleRowPerTarget()
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();
        var comment = fixture.AddComment(post);
        var userId = Guid.NewGuid();

        await fixture.Service.SetPostReactionAsync(post.Id, userId, new AddOrUpdateCommunityReactionRequest("like", "like"), CancellationToken.None);
        var updatedPost = await fixture.Service.SetPostReactionAsync(post.Id, userId, new AddOrUpdateCommunityReactionRequest("love", "love"), CancellationToken.None);
        await fixture.Service.SetCommentReactionAsync(comment.Id, userId, new AddOrUpdateCommunityReactionRequest("like", "like"), CancellationToken.None);
        var updatedComment = await fixture.Service.SetCommentReactionAsync(comment.Id, userId, new AddOrUpdateCommunityReactionRequest("love", "love"), CancellationToken.None);

        updatedPost.Should().ContainSingle(x => x.EmojiCode == "love" && x.Count == 1);
        updatedComment.Should().ContainSingle(x => x.EmojiCode == "love" && x.Count == 1);
        (await fixture.Db.CommunityPostReactions.CountAsync()).Should().Be(1);
        (await fixture.Db.CommunityCommentReactions.CountAsync()).Should().Be(1);
        (await fixture.Service.RemovePostReactionAsync(post.Id, userId, CancellationToken.None)).Should().BeEmpty();
        (await fixture.Service.RemoveCommentReactionAsync(comment.Id, userId, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_ShouldRejectOwnTargetAndTreatDuplicateAsConflictSignal()
    {
        using var fixture = new CommunityTestFixture();
        var ownerId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var post = fixture.AddPost(ownerId);
        var comment = fixture.AddComment(post, ownerId);
        var validPostReport = new ReportCommunityPostRequest("Spam", new string('a', 1000));
        var ownPostReport = () => fixture.Service.ReportPostAsync(post.Id, ownerId, validPostReport, CancellationToken.None);
        var ownCommentReport = () => fixture.Service.ReportCommentAsync(comment.Id, ownerId, new ReportCommunityCommentRequest("Spam", null), CancellationToken.None);
        var tooLongDescription = () => fixture.Service.ReportPostAsync(post.Id, Guid.NewGuid(), new ReportCommunityPostRequest("Spam", new string('a', 1001)), CancellationToken.None);

        (await fixture.Service.ReportPostAsync(post.Id, reporterId, validPostReport, CancellationToken.None)).Should().BeTrue();
        (await fixture.Service.ReportPostAsync(post.Id, reporterId, validPostReport, CancellationToken.None)).Should().BeFalse();
        (await fixture.Service.ReportCommentAsync(comment.Id, reporterId, new ReportCommunityCommentRequest("Spam", "reason"), CancellationToken.None)).Should().BeTrue();
        await ownPostReport.Should().ThrowAsync<ArgumentException>().WithMessage("cannot_report_own_post");
        await ownCommentReport.Should().ThrowAsync<ArgumentException>().WithMessage("cannot_report_own_comment");
        await tooLongDescription.Should().ThrowAsync<ArgumentException>().WithMessage("report_description_max_length");
    }
}
