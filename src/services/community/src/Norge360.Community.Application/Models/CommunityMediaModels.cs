namespace Norge360.Community.Application.Models;

public sealed record CommunityMediaUploadPayload(string FileName, string ContentType, byte[] Bytes, int Order);
public sealed record CommunityUploadedMedia(string StorageKey, string PublicUrl, string ContentType, long SizeBytes, int Width, int Height);
