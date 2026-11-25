using Cinema.Application.DTOs.Movies;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Interfaces.Recommendations;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Recommendations;

public class MovieRecommendationService(IAppDbContext db, IPricingService pricingService)
    : IMovieRecommendationService
{
    private sealed record PersonalContext(
        HashSet<int> ExcludedMovieIds,
        TasteLookup Taste,
        ShowtimeHabit Habit
    );

    private sealed record ShowtimeRow(int ShowtimeId, int MovieId, DateTime StartTime, int? ScreenTypeId);

    private sealed record SeatCount(int Total, int Available);

    public Task<MovieRecommendationsDto> GetForYouAsync(
        int? userId,
        int? limit,
        CancellationToken cancellationToken
    ) => GetForYouAsync(userId, limit, null, cancellationToken);

    public async Task<MovieRecommendationsDto> GetForYouAsync(
        int? userId,
        int? limit,
        RecommendationWindow? window,
        CancellationToken cancellationToken
    )
    {
        var take = Math.Clamp(
            limit ?? RecommendationWeights.DefaultLimit,
            1,
            RecommendationWeights.MaxLimit
        );
        var now = DateTime.UtcNow;
        var lookaheadEnd = now.AddDays(await db.LoadShowtimeLookaheadDaysAsync(cancellationToken));
        var (rangeStart, rangeEnd) = ResolveRange(window, now, lookaheadEnd);

        var personal = userId is { } id ? await LoadPersonalAsync(id, cancellationToken) : null;
        var personalized = personal?.Taste.HasSignals == true;

        var candidatesWindow = db
            .Showtimes.AsNoTracking()
            .WhereRegularKind(db)
            .Where(s =>
                s.Status == ShowtimeStatus.Scheduled
                && s.StartTime >= rangeStart
                && s.StartTime <= rangeEnd
                && s.Movie.Status == MovieStatus.NowShowing
            );
        if (personal is { ExcludedMovieIds.Count: > 0 })
        {
            var excluded = personal.ExcludedMovieIds.ToList();
            candidatesWindow = candidatesWindow.Where(s => !excluded.Contains(s.MovieId));
        }

        var rows = await candidatesWindow
            .Select(s => new ShowtimeRow(s.ShowtimeId, s.MovieId, s.StartTime, s.Room.ScreenTypeId))
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new MovieRecommendationsDto(personalized, []);

        var seatCounts = await db
            .ShowtimeSeats.AsNoTracking()
            .WhereSellable()
            .Where(ss => candidatesWindow.Any(s => s.ShowtimeId == ss.ShowtimeId))
            .GroupBy(ss => ss.ShowtimeId)
            .Select(g => new
            {
                ShowtimeId = g.Key,
                Total = g.Count(),
                Available = g.Count(x => x.Status == ShowtimeSeatStatus.Available),
            })
            .ToDictionaryAsync(
                x => x.ShowtimeId,
                x => new SeatCount(x.Total, x.Available),
                cancellationToken
            );

        var movieIds = rows.Select(r => r.MovieId).Distinct().ToList();
        var candidates = await LoadCandidatesAsync(
            movieIds,
            rows,
            seatCounts,
            personalized,
            cancellationToken
        );

        var taste = personalized ? personal!.Taste : TasteLookup.Empty;
        var similarity = personalized
            ? await LoadSimilarityAsync(personal!.ExcludedMovieIds, movieIds, cancellationToken)
            : [];

        var ranked = MovieRecommendationScorer
            .Rank(
                candidates.Select(c =>
                    MovieRecommendationScorer.Score(c, taste, similarity.GetValueOrDefault(c.MovieId))
                )
            )
            .Take(take)
            .ToList();

        var items = await BuildItemsAsync(
            ranked,
            rows,
            seatCounts,
            personalized ? personal!.Habit : ShowtimeHabit.Empty,
            now,
            cancellationToken
        );
        return new MovieRecommendationsDto(personalized, items);
    }

    private static (DateTime Start, DateTime End) ResolveRange(
        RecommendationWindow? window,
        DateTime now,
        DateTime lookaheadEnd
    )
    {
        if (window?.Date is not { } date)
            return (now, lookaheadEnd);

        var dayStart = BusinessTime.StartOfDayUtc(date);
        var dayEnd = BusinessTime.StartOfNextDayUtc(date).AddTicks(-1);
        var start = window.FromTime is { } time ? BusinessTime.ToUtc(date, time) : dayStart;
        return (start > now ? start : now, dayEnd < lookaheadEnd ? dayEnd : lookaheadEnd);
    }

    private async Task<PersonalContext?> LoadPersonalAsync(int userId, CancellationToken cancellationToken)
    {
        var optedIn = await db
            .CustomerProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (bool?)p.PersonalizedRecommendations)
            .FirstOrDefaultAsync(cancellationToken);
        if (optedIn is not true)
            return null;

        var bought = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.Order.UserId == userId
                && l.Order.Status == OrderStatus.Confirmed
                && l.RefundedQuantity < l.Quantity
                && l.ShowtimeId != null
                && l.Showtime!.Status != ShowtimeStatus.Cancelled
            )
            .Select(l => l.Showtime!.MovieId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var reviewed = await db
            .Reviews.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => r.MovieId)
            .ToListAsync(cancellationToken);

        var signals = await db
            .CustomerTasteSignals.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
        var typicalPrice = await db
            .CustomerTasteProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.TypicalTicketPrice)
            .FirstOrDefaultAsync(cancellationToken);

        return new PersonalContext(
            [.. bought, .. reviewed],
            new TasteLookup(signals),
            ShowtimeHabit.From(signals, typicalPrice)
        );
    }

    private async Task<List<CandidateMovie>> LoadCandidatesAsync(
        List<int> movieIds,
        List<ShowtimeRow> rows,
        Dictionary<int, SeatCount> seatCounts,
        bool withTaste,
        CancellationToken cancellationToken
    )
    {
        var movies = await db
            .Movies.AsNoTracking()
            .Where(m => movieIds.Contains(m.MovieId))
            .Select(m => new { m.MovieId, m.DirectorId, m.AverageRating })
            .ToListAsync(cancellationToken);
        var popularity = await db
            .MoviePopularities.AsNoTracking()
            .Where(p => movieIds.Contains(p.MovieId))
            .ToDictionaryAsync(p => p.MovieId, p => p.Score, cancellationToken);

        var genres = new Dictionary<int, List<int>>();
        var actors = new Dictionary<int, List<int>>();
        if (withTaste)
        {
            genres = (
                await db
                    .MovieGenres.AsNoTracking()
                    .Where(g => movieIds.Contains(g.MovieId))
                    .Select(g => new { g.MovieId, g.GenreId })
                    .ToListAsync(cancellationToken)
            )
                .GroupBy(g => g.MovieId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.GenreId).ToList());
            actors = (
                await db
                    .MovieCast.AsNoTracking()
                    .Where(c => movieIds.Contains(c.MovieId))
                    .Select(c => new { c.MovieId, c.ActorId })
                    .ToListAsync(cancellationToken)
            )
                .GroupBy(c => c.MovieId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ActorId).ToList());
        }

        var availableMovies = rows
            .Where(r => seatCounts.GetValueOrDefault(r.ShowtimeId)?.Available > 0)
            .Select(r => r.MovieId)
            .ToHashSet();

        return movies
            .Select(m => new CandidateMovie(
                m.MovieId,
                withTaste ? m.DirectorId : null,
                genres.GetValueOrDefault(m.MovieId) ?? [],
                actors.GetValueOrDefault(m.MovieId) ?? [],
                availableMovies.Contains(m.MovieId),
                popularity.GetValueOrDefault(m.MovieId),
                m.AverageRating
            ))
            .ToList();
    }

    private async Task<Dictionary<int, SimilarityHit>> LoadSimilarityAsync(
        HashSet<int> watchedMovieIds,
        List<int> candidateIds,
        CancellationToken cancellationToken
    )
    {
        if (watchedMovieIds.Count == 0)
            return [];

        var watched = watchedMovieIds.ToList();
        var hits = await db
            .MovieSimilarities.AsNoTracking()
            .Where(x => watched.Contains(x.MovieId) && candidateIds.Contains(x.SimilarMovieId))
            .Select(x => new
            {
                x.MovieId,
                x.SimilarMovieId,
                x.Score,
            })
            .ToListAsync(cancellationToken);

        return hits
            .GroupBy(x => x.SimilarMovieId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var best = g.OrderByDescending(x => x.Score).ThenBy(x => x.MovieId).First();
                    return new SimilarityHit(best.MovieId, best.Score);
                }
            );
    }

    private async Task<List<RecommendedMovieDto>> BuildItemsAsync(
        List<ScoredMovie> ranked,
        List<ShowtimeRow> rows,
        Dictionary<int, SeatCount> seatCounts,
        ShowtimeHabit habit,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        if (ranked.Count == 0)
            return [];

        var movieIds = ranked.Select(r => r.MovieId).ToList();
        var movieDtos = await db
            .Movies.AsNoTracking()
            .Where(m => movieIds.Contains(m.MovieId))
            .ProjectToType<MovieListItemDto>()
            .ToDictionaryAsync(m => m.MovieId, cancellationToken);

        var selectedRows = rows.Where(r => movieIds.Contains(r.MovieId)).ToList();
        var fromPrices = await pricingService.GetFromPricesAsync(
            selectedRows.Select(r => r.ShowtimeId).ToList(),
            now,
            cancellationToken
        );

        var chosen = selectedRows
            .GroupBy(r => r.MovieId)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.OrderByDescending(r => seatCounts.GetValueOrDefault(r.ShowtimeId)?.Available > 0)
                        .ThenByDescending(r =>
                            habit.Fit(
                                r.StartTime,
                                r.ScreenTypeId,
                                fromPrices.GetValueOrDefault(r.ShowtimeId)?.FromPrice
                            )
                        )
                        .ThenBy(r => r.StartTime)
                        .ThenBy(r => r.ShowtimeId)
                        .Take(RecommendationWeights.ShowtimesPerMovie)
                        .Select(r => r.ShowtimeId)
                        .ToList()
            );

        var chosenIds = chosen.Values.SelectMany(x => x).ToList();
        var showtimeEntities = await db
            .Showtimes.AsNoTracking()
            .Include(s => s.Room)
                .ThenInclude(r => r.ScreenType)
            .Where(s => chosenIds.Contains(s.ShowtimeId))
            .ToDictionaryAsync(s => s.ShowtimeId, cancellationToken);

        var names = await LoadReasonNamesAsync(ranked, cancellationToken);

        return ranked
            .Where(r => movieDtos.ContainsKey(r.MovieId))
            .Select(r =>
            {
                var showtimes = chosen
                    .GetValueOrDefault(r.MovieId, [])
                    .Where(showtimeEntities.ContainsKey)
                    .Select(showtimeId =>
                    {
                        var counts = seatCounts.GetValueOrDefault(showtimeId);
                        return showtimeEntities[showtimeId].Adapt<UpcomingShowtimeDto>() with
                        {
                            AvailableSeats = counts?.Available ?? 0,
                            TotalSeats = counts?.Total ?? 0,
                            FromPrice = fromPrices.GetValueOrDefault(showtimeId)?.FromPrice,
                        };
                    })
                    .ToList();

                return new RecommendedMovieDto(
                    movieDtos[r.MovieId],
                    ReasonText(r, names),
                    r.ReasonType.ToString(),
                    showtimes
                );
            })
            .ToList();
    }

    private async Task<Dictionary<(RecommendationReasonType Type, int Id), string>> LoadReasonNamesAsync(
        List<ScoredMovie> ranked,
        CancellationToken cancellationToken
    )
    {
        List<int> IdsOf(RecommendationReasonType type) =>
            ranked.Where(r => r.ReasonType == type && r.RefId > 0).Select(r => r.RefId).Distinct().ToList();

        var directorIds = IdsOf(RecommendationReasonType.Director);
        var genreIds = IdsOf(RecommendationReasonType.Genre);
        var actorIds = IdsOf(RecommendationReasonType.Actor);
        var movieIds = IdsOf(RecommendationReasonType.SimilarMovie);

        var names = new Dictionary<(RecommendationReasonType, int), string>();
        if (directorIds.Count > 0)
            foreach (
                var d in await db
                    .Directors.AsNoTracking()
                    .Where(d => directorIds.Contains(d.DirectorId))
                    .Select(d => new { d.DirectorId, d.FullName })
                    .ToListAsync(cancellationToken)
            )
                names[(RecommendationReasonType.Director, d.DirectorId)] = d.FullName;
        if (genreIds.Count > 0)
            foreach (
                var g in await db
                    .Genres.AsNoTracking()
                    .Where(g => genreIds.Contains(g.GenreId))
                    .Select(g => new { g.GenreId, g.Name })
                    .ToListAsync(cancellationToken)
            )
                names[(RecommendationReasonType.Genre, g.GenreId)] = g.Name;
        if (actorIds.Count > 0)
            foreach (
                var a in await db
                    .Actors.AsNoTracking()
                    .Where(a => actorIds.Contains(a.ActorId))
                    .Select(a => new { a.ActorId, a.FullName })
                    .ToListAsync(cancellationToken)
            )
                names[(RecommendationReasonType.Actor, a.ActorId)] = a.FullName;
        if (movieIds.Count > 0)
            foreach (
                var m in await db
                    .Movies.AsNoTracking()
                    .Where(m => movieIds.Contains(m.MovieId))
                    .Select(m => new { m.MovieId, m.Title })
                    .ToListAsync(cancellationToken)
            )
                names[(RecommendationReasonType.SimilarMovie, m.MovieId)] = m.Title;
        return names;
    }

    private static string ReasonText(
        ScoredMovie scored,
        Dictionary<(RecommendationReasonType Type, int Id), string> names
    )
    {
        if (
            scored.ReasonType != RecommendationReasonType.Popular
            && names.TryGetValue((scored.ReasonType, scored.RefId), out var name)
        )
            return scored.ReasonType switch
            {
                RecommendationReasonType.Director => string.Format(Messages.Recommendation_Reason_Director, name),
                RecommendationReasonType.Genre => string.Format(Messages.Recommendation_Reason_Genre, name),
                RecommendationReasonType.Actor => string.Format(Messages.Recommendation_Reason_Actor, name),
                _ => string.Format(Messages.Recommendation_Reason_SimilarMovie, name),
            };

        return Messages.Recommendation_Reason_Popular;
    }
}
