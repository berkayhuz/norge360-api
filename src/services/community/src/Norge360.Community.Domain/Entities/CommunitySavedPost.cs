using Norge360.Entities; namespace Norge360.Community.Domain.Entities; public sealed class CommunitySavedPost : AuditableEntity { public Guid PostId { get; set; } public Guid UserId { get; set; } }
