// <copyright file="CommunityServicePostTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Community.Contracts.Requests;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityServicePostTests
{
    [Fact]
    public async Task CreatePost_ShouldAcceptCaptionAtMaximumLength()
    {
        using var fixture = new CommunityTestFixture();

        var result = await fixture.Service.CreatePostAsync(Guid.NewGuid(), new CreateCommunityPostRequest(new string('a', 2200), "Oslo", "Sentrum"), CancellationToken.None);

        result.Caption.Should().HaveLength(2200);
    }

    [Fact]
    public async Task CreatePost_ShouldRejectCaptionAboveMaximumLength()
    {
        using var fixture = new CommunityTestFixture();

        var action = () => fixture.Service.CreatePostAsync(Guid.NewGuid(), new CreateCommunityPostRequest(new string('a', 2201), "Oslo", "Sentrum"), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("caption_max_length");
    }

    [Fact]
    public async Task UpdateAndDelete_ShouldRejectNonOwnerAndAllowModerator()
    {
        using var fixture = new CommunityTestFixture();
        var post = fixture.AddPost();
        var nonOwner = Guid.NewGuid();

        var update = () => fixture.Service.UpdatePostAsync(post.Id, nonOwner, false, new UpdateCommunityPostRequest("updated", "Oslo", "Sentrum"), CancellationToken.None);
        var delete = () => fixture.Service.DeletePostAsync(post.Id, nonOwner, false, CancellationToken.None);

        await update.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("post_update_forbidden");
        await delete.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("post_delete_forbidden");
        (await fixture.Service.UpdatePostAsync(post.Id, nonOwner, true, new UpdateCommunityPostRequest("moderated", "Bergen", "Bergenhus"), CancellationToken.None))!.Caption.Should().Be("moderated");
        (await fixture.Service.DeletePostAsync(post.Id, nonOwner, true, CancellationToken.None)).Should().BeTrue();
        (await fixture.Service.GetPostAsync(post.Id, nonOwner, CancellationToken.None)).Should().BeNull();
    }
}
