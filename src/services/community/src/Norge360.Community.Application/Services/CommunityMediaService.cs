using Microsoft.Extensions.Options;
using Norge360.Community.Application.Abstractions;
using Norge360.Community.Application.Models;
using Norge360.Media;
using Norge360.Media.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Norge360.Community.Application.Services;

public sealed class CommunityMediaService(
    IImageValidator imageValidator,
    IMediaStorageProvider storageProvider,
    IMediaUrlBuilder mediaUrlBuilder,
    IOptions<Media.Options.MediaOptions> mediaOptions) : ICommunityMediaService
{
    private const int TargetMaxBytes = 1 * 1024 * 1024;
    private const int InputMaxBytes = 15 * 1024 * 1024;

    public async Task<IReadOnlyList<CommunityUploadedMedia>> UploadPostMediaAsync(Guid postId, Guid userId, IReadOnlyList<CommunityMediaUploadPayload> files, CancellationToken cancellationToken)
    {
        var uploaded = new List<CommunityUploadedMedia>(files.Count);
        var uploadedKeys = new List<string>(files.Count);
        try
        {
            foreach (var file in files.OrderBy(x => x.Order))
            {
                if (file.Bytes.Length == 0) throw new ArgumentException("community_media_invalid_image");
                if (file.Bytes.Length > InputMaxBytes) throw new ArgumentException("community_media_input_too_large");

                await using var input = new MemoryStream(file.Bytes);
                var validation = await imageValidator.ValidateAsync(file.FileName, file.ContentType, input, input.Length, cancellationToken);
                if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.CanonicalContentType)) throw new ArgumentException("community_media_invalid_type");

                input.Position = 0;
                await using var output = await OptimizeIfNeededAsync(input, validation.CanonicalContentType, cancellationToken);

                var ext = validation.CanonicalContentType switch { "image/jpeg" => "jpg", "image/png" => "png", _ => "webp" };
                var key = $"community/posts/{postId}/{Guid.NewGuid():N}.{ext}";
                output.Position = 0;
                await storageProvider.SaveAsync(key, output, validation.CanonicalContentType, cancellationToken);
                uploadedKeys.Add(key);

                output.Position = 0;
                using var image = await Image.LoadAsync(output, cancellationToken);
                uploaded.Add(new CommunityUploadedMedia(key, mediaUrlBuilder.BuildPublicUrl(key), validation.CanonicalContentType, output.Length, image.Width, image.Height));
            }

            return uploaded;
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try { await storageProvider.DeleteAsync(key, cancellationToken); } catch { }
            }
            throw;
        }
    }

    public async Task<bool> DeleteMediaByStorageKeyAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await storageProvider.DeleteAsync(storageKey, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<MemoryStream> OptimizeIfNeededAsync(Stream source, string contentType, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(source, cancellationToken);
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;

        if (image.Width > 1920 || image.Height > 1920)
        {
            var ratio = Math.Min(1920d / image.Width, 1920d / image.Height);
            var w = Math.Max(1, (int)Math.Round(image.Width * ratio));
            var h = Math.Max(1, (int)Math.Round(image.Height * ratio));
            image.Mutate(x => x.Resize(w, h));
        }

        var output = new MemoryStream();
        await SaveWithQualityAsync(image, contentType, output, 90, cancellationToken);

        var quality = 82;
        while (output.Length > TargetMaxBytes && quality >= 45)
        {
            output.SetLength(0);
            output.Position = 0;
            await SaveWithQualityAsync(image, contentType, output, quality, cancellationToken);
            quality -= 7;
        }

        if (output.Length > TargetMaxBytes)
        {
            throw new MediaValidationException("community_media_optimization_failed");
        }

        output.Position = 0;
        return output;
    }

    private static Task SaveWithQualityAsync(Image image, string contentType, Stream target, int quality, CancellationToken cancellationToken)
    {
        return contentType switch
        {
            "image/jpeg" => image.SaveAsync(target, new JpegEncoder { Quality = quality }, cancellationToken),
            "image/png" => image.SaveAsync(target, new PngEncoder(), cancellationToken),
            _ => image.SaveAsync(target, new WebpEncoder { Quality = quality }, cancellationToken)
        };
    }
}
