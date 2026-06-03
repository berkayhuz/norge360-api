using Norge360.Community.Application.Models;

namespace Norge360.Community.Application.Abstractions;

public interface ICommunityAuthorProfileProvider
{
    Task<IReadOnlyDictionary<Guid, CommunityAuthorSummary>> GetAuthorSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid? currentUserId,
        CancellationToken cancellationToken);
}
