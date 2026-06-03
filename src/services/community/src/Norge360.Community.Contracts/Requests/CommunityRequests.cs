namespace Norge360.Community.Contracts.Requests;

public sealed record CreateCommunityPostRequest(string? Caption, string? City, string? District);
public sealed record UpdateCommunityPostRequest(string? Caption, string? City, string? District);
public sealed record CreateCommunityCommentRequest(string Body);
public sealed record CreateCommunityReplyRequest(string Body);
public sealed record SetCommunityPostInterestRequest(string InterestType);
public sealed record AddOrUpdateCommunityReactionRequest(string Emoji, string EmojiCode);
public sealed record ReportCommunityPostRequest(string Reason, string? Description);
public sealed record ReportCommunityCommentRequest(string Reason, string? Description);
