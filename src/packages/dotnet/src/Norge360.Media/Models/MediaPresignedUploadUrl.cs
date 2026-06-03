// <copyright file="MediaPresignedUploadUrl.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Media.Models;

public sealed record MediaPresignedUploadUrl(
    string UploadUrl,
    string Method,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string>? Headers);

