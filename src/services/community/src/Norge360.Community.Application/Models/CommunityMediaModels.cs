// <copyright file="CommunityMediaModels.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Community.Application.Models;

public sealed record CommunityMediaUploadPayload(string FileName, string ContentType, byte[] Bytes, int Order);
public sealed record CommunityUploadedMedia(string StorageKey, string PublicUrl, string ContentType, long SizeBytes, int Width, int Height);
