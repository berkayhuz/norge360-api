// <copyright file="CommunityServiceMediaTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Norge360.Community.Application.Models;
using Norge360.Community.Application.Services;
using Norge360.Media.Abstractions;
using Norge360.Media;
using Norge360.Media.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityServiceMediaTests
{
    [Fact]
    public async Task Upload_ShouldRejectInputAboveFifteenMegabytes()
    {
        var harness = new MediaHarness();
        var payload = new CommunityMediaUploadPayload("large.png", "image/png", new byte[(15 * 1024 * 1024) + 1], 0);

        var action = () => harness.Service.UploadPostMediaAsync(Guid.NewGuid(), Guid.NewGuid(), [payload], CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("community_media_input_too_large");
        harness.Storage.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/svg+xml")]
    public async Task Upload_ShouldRejectUnsupportedContentType(string contentType)
    {
        var harness = new MediaHarness();
        harness.Validator.Setup(x => x.ValidateAsync(It.IsAny<string>(), contentType, It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageValidationResult(false, "unsupported", null, null));

        var action = () => harness.Service.UploadPostMediaAsync(Guid.NewGuid(), Guid.NewGuid(), [new("file.bin", contentType, CreatePng(), 0)], CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("community_media_invalid_type");
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task Upload_ShouldAcceptSupportedImageTypes(string contentType)
    {
        var harness = new MediaHarness();

        var uploaded = await harness.Service.UploadPostMediaAsync(Guid.NewGuid(), Guid.NewGuid(), [new("file.png", contentType, CreatePng(), 0)], CancellationToken.None);

        uploaded.Should().ContainSingle(x => x.ContentType == contentType && x.Width == 8 && x.Height == 8);
        harness.Storage.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), contentType, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_ShouldRejectCorruptImageAfterValidation()
    {
        var harness = new MediaHarness();

        var action = () => harness.Service.UploadPostMediaAsync(Guid.NewGuid(), Guid.NewGuid(), [new("file.png", "image/png", [1, 2, 3], 0)], CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
        harness.Storage.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ShouldPreserveOrderAndCleanupEarlierObjectWhenStorageFails()
    {
        var harness = new MediaHarness();
        var savedContentTypes = new List<string>();
        harness.Storage.Setup(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, Stream, string, CancellationToken>((_, _, contentType, _) => savedContentTypes.Add(contentType))
            .Returns((string _, Stream _, string contentType, CancellationToken _) =>
                contentType == "image/jpeg" ? Task.FromException(new InvalidOperationException("storage_failed")) : Task.CompletedTask);

        var action = () => harness.Service.UploadPostMediaAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new("second.jpg", "image/jpeg", CreatePng(), 2),
                new("first.png", "image/png", CreatePng(), 1)
            ],
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("storage_failed");
        savedContentTypes.Should().Equal("image/png", "image/jpeg");
        harness.Storage.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_ShouldFailWhenOptimizedImageStillExceedsOneMegabyte()
    {
        var harness = new MediaHarness();

        var action = () => harness.Service.UploadPostMediaAsync(Guid.NewGuid(), Guid.NewGuid(), [new("noise.png", "image/png", CreateNoisyPng(), 0)], CancellationToken.None);

        await action.Should().ThrowAsync<MediaValidationException>().WithMessage("community_media_optimization_failed");
        harness.Storage.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static byte[] CreatePng()
    {
        using var image = new Image<Rgba32>(8, 8);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static byte[] CreateNoisyPng()
    {
        using var image = new Image<Rgba32>(1024, 1024);
        var random = new Random(42);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), byte.MaxValue);
                }
            }
        });
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private sealed class MediaHarness
    {
        public MediaHarness()
        {
            Validator.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, string contentType, Stream _, long _, CancellationToken _) => new ImageValidationResult(true, null, contentType, ".png"));
            Storage.SetupGet(x => x.Name).Returns("test");
            Storage.Setup(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Storage.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UrlBuilder.Setup(x => x.BuildPublicUrl(It.IsAny<string>()))
                .Returns((string key) => $"https://cdn.test/{key}");

            Service = new CommunityMediaService(Validator.Object, Storage.Object, UrlBuilder.Object);
        }

        public Mock<IImageValidator> Validator { get; } = new();
        public Mock<IMediaStorageProvider> Storage { get; } = new();
        public Mock<IMediaUrlBuilder> UrlBuilder { get; } = new();
        public CommunityMediaService Service { get; }
    }
}
