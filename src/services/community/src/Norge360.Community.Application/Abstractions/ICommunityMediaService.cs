using Norge360.Community.Application.Models;

namespace Norge360.Community.Application.Abstractions;

public interface ICommunityMediaService
{
    Task<IReadOnlyList<CommunityUploadedMedia>> UploadPostMediaAsync(Guid postId, Guid userId, IReadOnlyList<CommunityMediaUploadPayload> files, CancellationToken cancellationToken);
    Task<bool> DeleteMediaByStorageKeyAsync(string storageKey, CancellationToken cancellationToken);
}
