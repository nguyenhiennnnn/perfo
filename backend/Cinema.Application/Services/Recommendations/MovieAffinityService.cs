using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Recommendations;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Recommendations;

public class MovieAffinityService(IAppDbContext db) : IMovieAffinityService
{
    private const long RefreshLockKey = 1_501_002;

    public Task RefreshAsync(CancellationToken cancellationToken) =>
        db.InTransactionAsync(
            async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({RefreshLockKey})",
                    cancellationToken
                );

                var now = DateTime.UtcNow;
                var similarities = await ComputeSimilaritiesAsync(cancellationToken);
                var popularities = await ComputePopularitiesAsync(now, cancellationToken);
                await ReplaceAsync(similarities, popularities, now, cancellationToken);
            },
            cancellationToken
        );

    private async Task<List<MovieSimilarityResult>> ComputeSimilaritiesAsync(
        CancellationToken cancellationToken
    )
    {
        var views = db
            .OrderLines.AsNoTracking()
            .CheckedInViewings(db)
            .Where(l =>
                l.Order.UserId != null
                && db.CustomerProfiles.Any(p =>
                    p.UserId == l.Order.UserId && p.PersonalizedRecommendations
                )
            )
            .Select(l => new { UserId = l.Order.UserId!.Value, l.Showtime!.MovieId })
            .Distinct();

        var viewersByMovie = await views
            .GroupBy(v => v.MovieId)
            .Select(g => new { MovieId = g.Key, Viewers = g.Count() })
            .ToDictionaryAsync(x => x.MovieId, x => x.Viewers, cancellationToken);
        if (viewersByMovie.Count < 2)
            return [];

        var pairRows = await (
            from a in views
            join b in views on a.UserId equals b.UserId
            where a.MovieId < b.MovieId
            group a by new { MovieA = a.MovieId, MovieB = b.MovieId } into g
            select new
            {
                g.Key.MovieA,
                g.Key.MovieB,
                CoViewers = g.Count(),
            }
        )
            .Where(p => p.CoViewers >= MovieAffinityWeights.MinCoViewers)
            .ToListAsync(cancellationToken);
        var pairs = pairRows.Select(p => new MoviePairViewers(p.MovieA, p.MovieB, p.CoViewers));

        var recommendable = (
            await db
                .Movies.AsNoTracking()
                .Where(m => m.Status != MovieStatus.Ended)
                .Select(m => m.MovieId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        return MovieSimilarityCalculator.Calculate(viewersByMovie, pairs, recommendable);
    }

    private async Task<List<MoviePopularityResult>> ComputePopularitiesAsync(
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var since = now.AddDays(-MovieAffinityWeights.PopularityWindowDays);

        var movieIds = await db
            .Movies.AsNoTracking()
            .Where(m => m.Status != MovieStatus.ComingSoon)
            .Select(m => m.MovieId)
            .ToListAsync(cancellationToken);
        if (movieIds.Count == 0)
            return [];

        var tickets = await db
            .OrderLines.AsNoTracking()
            .CheckedInViewings(db)
            .Where(l =>
                l.Showtime!.Kind != ShowtimeKind.Event
                && (l.CheckedInAt ?? l.Showtime!.StartTime) >= since
            )
            .Select(l => new PopularityTicket(
                l.Showtime!.MovieId,
                l.CheckedInAt ?? l.Showtime!.StartTime,
                l.Quantity - l.RefundedQuantity
            ))
            .ToListAsync(cancellationToken);

        var reviews = await db
            .Reviews.AsNoTracking()
            .Where(r => r.IsVerifiedViewer)
            .GroupBy(r => r.MovieId)
            .Select(g => new
            {
                MovieId = g.Key,
                Count = g.Count(),
                Sum = g.Sum(r => r.Rating),
            })
            .ToDictionaryAsync(
                x => x.MovieId,
                x => new PopularityReviews(x.Count, x.Sum),
                cancellationToken
            );

        return MoviePopularityCalculator.Calculate(movieIds, tickets, reviews, now);
    }

    private async Task ReplaceAsync(
        List<MovieSimilarityResult> similarities,
        List<MoviePopularityResult> popularities,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        await db.MovieSimilarities.ExecuteDeleteAsync(cancellationToken);
        await db.MoviePopularities.ExecuteDeleteAsync(cancellationToken);

        db.MovieSimilarities.AddRange(
            similarities.Select(s => new MovieSimilarity
            {
                MovieId = s.MovieId,
                SimilarMovieId = s.SimilarMovieId,
                Score = s.Score,
                CoViewers = s.CoViewers,
                UpdatedAt = now,
            })
        );
        db.MoviePopularities.AddRange(
            popularities.Select(p => new MoviePopularity
            {
                MovieId = p.MovieId,
                RecentTickets = p.RecentTickets,
                ReviewCount = p.ReviewCount,
                AverageRating = p.AverageRating,
                Score = p.Score,
                UpdatedAt = now,
            })
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
