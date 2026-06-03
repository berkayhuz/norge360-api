// <copyright file="CommunityPost.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Domain.Enums;
using Norge360.Entities;
namespace Norge360.Community.Domain.Entities; public sealed class CommunityPost : AuditableEntity { public Guid UserId { get; set; } public string? Caption { get; set; } public string? City { get; set; } public string? District { get; set; } public CommunityPostStatus Status { get; set; } = CommunityPostStatus.Published; public ICollection<CommunityPostMedia> Media { get; set; } = []; public ICollection<CommunityComment> Comments { get; set; } = []; }
