// <copyright file="CommunityPostMedia.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Domain.Enums;
using Norge360.Entities;
namespace Norge360.Community.Domain.Entities; public sealed class CommunityPostMedia : AuditableEntity { public Guid PostId { get; set; } public CommunityPost Post { get; set; } = null!; public string StorageKey { get; set; } = null!; public string PublicUrl { get; set; } = null!; public string ContentType { get; set; } = null!; public long SizeBytes { get; set; } public int Width { get; set; } public int Height { get; set; } public short Order { get; set; } public CommunityMediaStatus Status { get; set; } = CommunityMediaStatus.Pending; }
