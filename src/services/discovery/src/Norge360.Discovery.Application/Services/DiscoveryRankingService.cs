using Microsoft.EntityFrameworkCore;
using Norge360.Discovery.Application.Abstractions;
using Norge360.Discovery.Contracts.Responses;
using Norge360.Discovery.Domain.Entities;
using Norge360.Discovery.Domain.Enums;

namespace Norge360.Discovery.Application.Services;

public sealed class DiscoveryRankingService(IDiscoveryDbContext dbContext) : IDiscoveryRankingService
{
    public async Task RecomputeAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await RecomputePopularUsersAsync(now, cancellationToken);
        await RecomputeTrendingUsersAsync(now, cancellationToken);
    }

    public Task<IReadOnlyList<DiscoverUserResponse>> GetPopularUsersAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default)
        => GetUsersAsync(DiscoveryRankingType.PopularUsers, NormalizeLimit(limit), "Bu hafta populer", cancellationToken);

    public Task<IReadOnlyList<DiscoverUserResponse>> GetTrendingUsersAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default)
        => GetUsersAsync(DiscoveryRankingType.TrendingUsers, NormalizeLimit(limit), "Yukselen profil", cancellationToken);

    public async Task<IReadOnlyList<DiscoverUserResponse>> GetFollowSuggestionsAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default)
    {
        var popular = await GetUsersAsync(DiscoveryRankingType.PopularUsers, NormalizeLimit(limit), "Ortak ilgi alanlari", cancellationToken);
        return popular.Where(x => x.UserId != viewerUserId).ToList();
    }

    public async Task<DiscoveryHubResponse> GetHubAsync(int limit, Guid? viewerUserId, CancellationToken cancellationToken = default)
    {
        var safeLimit = NormalizeLimit(limit);
        var popular = await GetPopularUsersAsync(safeLimit, viewerUserId, cancellationToken);
        var trending = await GetTrendingUsersAsync(safeLimit, viewerUserId, cancellationToken);
        var suggested = await GetFollowSuggestionsAsync(safeLimit, viewerUserId, cancellationToken);
        return new DiscoveryHubResponse(popular, trending, suggested);
    }

    private async Task RecomputePopularUsersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var weekStart = now.Date.AddDays(-7);
        var monthStart = now.Date.AddDays(-30);
        var aggregates = await dbContext.DiscoveryDailyAggregates
            .Where(x => x.TargetType == DiscoverySubjectType.User && x.Date >= DateOnly.FromDateTime(monthStart))
            .GroupBy(x => x.TargetId)
            .Select(g => new
            {
                TargetId = g.Key,
                WeeklyScore = g.Where(x => x.Date >= DateOnly.FromDateTime(weekStart)).Sum(x => x.RawScore),
                MonthlyScore = g.Sum(x => x.RawScore)
            })
            .ToListAsync(cancellationToken);

        var scored = aggregates
            .Select(x => new ScoredTarget(x.TargetId, x.WeeklyScore * 0.7m + x.MonthlyScore * 0.3m))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(500)
            .ToList();

        await ReplaceRankingsAsync(DiscoveryRankingType.PopularUsers, scored, monthStart, now, cancellationToken);
    }

    private async Task RecomputeTrendingUsersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(-72);
        var previousStart = now.AddDays(-7);
        var currentDate = DateOnly.FromDateTime(windowStart);
        var previousDate = DateOnly.FromDateTime(previousStart);

        var aggregates = await dbContext.DiscoveryDailyAggregates
            .Where(x => x.TargetType == DiscoverySubjectType.User && x.Date >= previousDate)
            .GroupBy(x => x.TargetId)
            .Select(g => new
            {
                TargetId = g.Key,
                RecentScore = g.Where(x => x.Date >= currentDate).Sum(x => x.RawScore),
                BaselineScore = g.Where(x => x.Date < currentDate).Sum(x => x.RawScore)
            })
            .ToListAsync(cancellationToken);

        var scored = aggregates
            .Select(x => new ScoredTarget(x.TargetId, x.RecentScore * 1.5m - x.BaselineScore * 0.15m))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(500)
            .ToList();

        await ReplaceRankingsAsync(DiscoveryRankingType.TrendingUsers, scored, windowStart, now, cancellationToken);
    }

    private async Task ReplaceRankingsAsync(
        DiscoveryRankingType rankingType,
        IReadOnlyList<ScoredTarget> scored,
        DateTime windowStart,
        DateTime windowEnd,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.DiscoveryRankings.Where(x => x.RankingType == rankingType).ToListAsync(cancellationToken);
        dbContext.DiscoveryRankings.RemoveRange(existing);

        var rank = 1;
        foreach (var item in scored)
        {
            dbContext.DiscoveryRankings.Add(new DiscoveryRanking
            {
                RankingType = rankingType,
                TargetType = DiscoverySubjectType.User,
                TargetId = item.TargetId,
                Score = item.Score,
                Rank = rank++,
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                ComputedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DiscoverUserResponse>> GetUsersAsync(
        DiscoveryRankingType rankingType,
        int limit,
        string reasonLabel,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from ranking in dbContext.DiscoveryRankings
            from snapshot in dbContext.DiscoverySubjectSnapshots
            where ranking.RankingType == rankingType
                  && ranking.TargetType == DiscoverySubjectType.User
                  && snapshot.SubjectType == DiscoverySubjectType.User
                  && (snapshot.SubjectId == ranking.TargetId || snapshot.AuthUserId == ranking.TargetId)
                  && snapshot.IsActive
                  && !snapshot.IsDeleted
                  && snapshot.Visibility == "Public"
                  && snapshot.Username != null
            orderby ranking.Rank
            select new
            {
                snapshot.AuthUserId,
                snapshot.SubjectId,
                snapshot.Username,
                snapshot.DisplayName,
                snapshot.AvatarUrl,
                snapshot.Bio,
                snapshot.IsVerified
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return await GetFallbackUsersAsync(limit, reasonLabel, cancellationToken);
        }

        return rows.Select(x => new DiscoverUserResponse(
            x.AuthUserId,
            x.SubjectId,
            x.Username ?? string.Empty,
            x.DisplayName,
            x.AvatarUrl,
            x.Bio,
            x.IsVerified,
            false,
            reasonLabel)).ToList();
    }

    private async Task<IReadOnlyList<DiscoverUserResponse>> GetFallbackUsersAsync(
        int limit,
        string reasonLabel,
        CancellationToken cancellationToken)
    {
        var fallbackQuery = dbContext.DiscoverySubjectSnapshots
            .Where(snapshot =>
                snapshot.SubjectType == DiscoverySubjectType.User &&
                snapshot.IsActive &&
                !snapshot.IsDeleted &&
                snapshot.Visibility == "Public" &&
                snapshot.Username != null);

        var rows = await fallbackQuery
            .Where(snapshot => snapshot.FollowersCount > 0 || snapshot.PostsCount > 0)
            .OrderByDescending(snapshot => snapshot.FollowersCount)
            .ThenByDescending(snapshot => snapshot.PostsCount)
            .ThenByDescending(snapshot => snapshot.UpdatedAt)
            .ThenBy(snapshot => snapshot.SubjectId)
            .Take(limit)
            .Select(snapshot => new
            {
                snapshot.AuthUserId,
                snapshot.SubjectId,
                snapshot.Username,
                snapshot.DisplayName,
                snapshot.AvatarUrl,
                snapshot.Bio,
                snapshot.IsVerified
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            var candidates = await fallbackQuery
                .OrderByDescending(snapshot => snapshot.UpdatedAt)
                .ThenBy(snapshot => snapshot.SubjectId)
                .Take(500)
                .Select(snapshot => new
                {
                    snapshot.AuthUserId,
                    snapshot.SubjectId,
                    snapshot.Username,
                    snapshot.DisplayName,
                    snapshot.AvatarUrl,
                    snapshot.Bio,
                    snapshot.IsVerified
                })
                .ToListAsync(cancellationToken);

            rows = candidates
                .OrderBy(_ => Random.Shared.Next())
                .Take(limit)
                .ToList();
        }

        return rows.Select(x => new DiscoverUserResponse(
            x.AuthUserId,
            x.SubjectId,
            x.Username ?? string.Empty,
            x.DisplayName,
            x.AvatarUrl,
            x.Bio,
            x.IsVerified,
            false,
            reasonLabel)).ToList();
    }

    private static int NormalizeLimit(int limit) => limit is <= 0 or > 50 ? 10 : limit;

    private sealed record ScoredTarget(Guid TargetId, decimal Score);
}
