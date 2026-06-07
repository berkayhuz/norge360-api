// <copyright file="NotificationTypes.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Notification.Contracts.Notifications;

public static class NotificationTypes
{
    public const string NewFollower = "social.new_follower";
    public const string FollowRequest = "social.follow_request";
    public const string FollowRequestAccepted = "social.follow_request_accepted";
    public const string ProfilePost = "community.profile_post";
    public const string CityPost = "community.city_post";
    public const string FollowedFirstPost = "community.followed_first_post";
    public const string PostLike = "community.post_like";
    public const string CommentLike = "community.comment_like";
    public const string PostComment = "community.post_comment";
    public const string CommentReply = "community.comment_reply";
}
