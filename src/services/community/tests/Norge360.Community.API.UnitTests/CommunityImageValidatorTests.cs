// <copyright file="CommunityImageValidatorTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Options;
using Norge360.Media.Options;
using Norge360.Media.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class CommunityImageValidatorTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public async Task Validate_ShouldAcceptSupportedImagePayloads(string contentType, string extension)
    {
        var validator = CreateValidator();
        var bytes = CreateImage(contentType);

        var result = await validator.ValidateAsync($"image{extension}", contentType, new MemoryStream(bytes), bytes.Length, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.CanonicalContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("image.gif", "image/gif")]
    [InlineData("image.svg", "image/svg+xml")]
    public async Task Validate_ShouldRejectUnsupportedExtensions(string fileName, string contentType)
    {
        var validator = CreateValidator();
        var bytes = new byte[64];

        var result = await validator.ValidateAsync(fileName, contentType, new MemoryStream(bytes), bytes.Length, CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ShouldRejectCorruptPayloadWithAllowedExtension()
    {
        var validator = CreateValidator();
        var bytes = new byte[64];

        var result = await validator.ValidateAsync("image.png", "image/png", new MemoryStream(bytes), bytes.Length, CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    private static DefaultImageValidator CreateValidator() =>
        new(Options.Create(new MediaOptions { MaxImageBytes = 15 * 1024 * 1024 }));

    private static byte[] CreateImage(string contentType)
    {
        using var image = new Image<Rgba32>(8, 8);
        using var stream = new MemoryStream();
        image.Save(stream, GetEncoder(contentType));
        return stream.ToArray();
    }

    private static IImageEncoder GetEncoder(string contentType) => contentType switch
    {
        "image/jpeg" => new JpegEncoder(),
        "image/png" => new PngEncoder(),
        _ => new WebpEncoder()
    };
}
