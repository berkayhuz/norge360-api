namespace Norge360.Community.Application.Abstractions; public interface ICommunityUnitOfWork { Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); }
