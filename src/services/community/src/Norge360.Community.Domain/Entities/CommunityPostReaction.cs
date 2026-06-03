// <copyright file="CommunityPostReaction.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Entities;
namespace Norge360.Community.Domain.Entities; public sealed class CommunityPostReaction : AuditableEntity { public Guid PostId { get; set; } public Guid UserId { get; set; } public string Emoji { get; set; } = null!; public string EmojiCode { get; set; } = null!; }
