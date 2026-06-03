using Norge360.Entities; namespace Norge360.Community.Domain.Entities; public sealed class CommunityPostLike : AuditableEntity { public Guid PostId { get; set; } public Guid UserId { get; set; } }
