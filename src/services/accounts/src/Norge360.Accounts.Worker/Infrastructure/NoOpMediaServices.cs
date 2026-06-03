// <copyright file="NoOpMediaServices.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Media.Abstractions;
using Norge360.Media.Models;

namespace Norge360.Accounts.Worker.Infrastructure;

internal sealed class NoOpMediaUploadUrlSigner : IMediaUploadUrlSigner
{
    public MediaPresignedUploadUrl CreatePresignedUploadUrl(MediaUploadUrlRequest request)
    {
        throw new InvalidOperationException("Media upload URL signing is not available in accounts-worker runtime.");
    }
}

internal sealed class NoOpMediaStorageProvider : IMediaStorageProvider
{
    public string Name => "noop";

    public Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Media storage is not available in accounts-worker runtime.");
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Media storage is not available in accounts-worker runtime.");
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
