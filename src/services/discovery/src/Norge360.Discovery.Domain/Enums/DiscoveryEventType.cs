namespace Norge360.Discovery.Domain.Enums;

public enum DiscoveryEventType
{
    Unknown = 0,
    ProfileFollowed = 1,
    ProfileUnfollowed = 2,
    ProfileViewed = 3,
    ProfileCreated = 4,
    ProfileUpdated = 5,
    ProfileVisibilityChanged = 6,
    ProfileBlocked = 7,
    ProfileUnblocked = 8,
    ProfileDeleted = 9,
    ProfileDeactivated = 10,
    ProfileReactivated = 11,
    PostCreated = 20,
    PostLiked = 21,
    PostUnliked = 22,
    PostCommented = 23,
    PostCommentDeleted = 24,
    PostDeleted = 25,
    PostHidden = 26,
    PostModerated = 27
}
