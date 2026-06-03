namespace Norge360.Community.Application.Abstractions;

public interface ICommunityVisibilityService
{
    Task<IReadOnlySet<Guid>> FilterVisibleAuthorsAsync(
        IReadOnlyCollection<Guid> authorUserIds,
        Guid? currentUserId,
        CancellationToken cancellationToken);

    Task<bool> CanViewAuthorPostsAsync(Guid authorUserId, Guid? currentUserId, CancellationToken cancellationToken);
}
