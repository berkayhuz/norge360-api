// <copyright file="CommunityComment.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Entities;

namespace Norge360.Community.Domain.Entities;

public sealed class CommunityComment : AuditableEntity
{
    public Guid PostId { get; set; }
    public CommunityPost Post { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Slug { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public CommunityComment? ParentComment { get; set; }
    public ICollection<CommunityComment> Replies { get; set; } = [];
    public string Body { get; set; } = null!;
}
