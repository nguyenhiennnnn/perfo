using Cinema.Application.Utils;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Recommendations;

internal sealed record TasteViewing(
    int ShowtimeId,
    int MovieId,
    DateTime StartTime,
    int? ScreenTypeId,
    bool CheckedIn,
    DateTime? CheckedInAt,
    decimal AveragePrice
);

internal sealed record TasteReview(int MovieId, int Rating, DateTime CreatedAt);

internal sealed record TasteMovie(
    int MovieId,
    int? DirectorId,
    IReadOnlyList<int> GenreIds,
    IReadOnlyList<int> ActorIds
);

internal sealed record TasteSignalValue(TasteSignalKind Kind, int RefId, decimal Weight);

internal sealed record TasteResult(
    IReadOnlyList<TasteSignalValue> Signals,
    decimal? TypicalTicketPrice,
    int MovieCount
);

internal static class TasteProfileCalculator
{
    public static TasteResult Calculate(
        IReadOnlyCollection<TasteViewing> viewings,
        IReadOnlyCollection<TasteReview> reviews,
        IReadOnlyDictionary<int, TasteMovie> movies,
        DateTime nowUtc
    )
    {
        var movieWeights = MovieWeights(viewings, reviews, nowUtc);
        var totals = new Dictionary<(TasteSignalKind Kind, int RefId), decimal>();

        foreach (var (movieId, weight) in movieWeights)
        {
            if (!movies.TryGetValue(movieId, out var movie))
                continue;

            foreach (var genreId in movie.GenreIds.Distinct())
                Add(totals, TasteSignalKind.Genre, genreId, weight);
            foreach (var actorId in movie.ActorIds.Distinct())
                Add(totals, TasteSignalKind.Actor, actorId, weight);
            if (movie.DirectorId is { } directorId)
                Add(totals, TasteSignalKind.Director, directorId, weight);
        }

        var slots = TimeSlots.All.ToList();
        decimal priceWeighted = 0m,
            priceWeight = 0m;
        foreach (var viewing in viewings)
        {
            var weight = TasteWeights.Recency(viewing.StartTime, nowUtc);
            var slot = slots.IndexOf(TimeSlots.Of(viewing.StartTime));
            Add(totals, TasteSignalKind.TimeSlot, slot, weight);
            if (viewing.ScreenTypeId is { } screenTypeId)
                Add(totals, TasteSignalKind.ScreenType, screenTypeId, weight);

            priceWeighted += viewing.AveragePrice * weight;
            priceWeight += weight;
        }

        decimal? typicalPrice =
            priceWeight > 0m
                ? Math.Round(priceWeighted / priceWeight, 0, MidpointRounding.AwayFromZero)
                : null;

        var signals = totals
            .Select(t => new TasteSignalValue(
                t.Key.Kind,
                t.Key.RefId,
                Math.Round(t.Value, 4, MidpointRounding.AwayFromZero)
            ))
            .Where(s => s.Weight >= TasteWeights.MinimumWeight)
            .OrderBy(s => s.Kind)
            .ThenBy(s => s.RefId)
            .ToList();

        return new TasteResult(signals, typicalPrice, movieWeights.Count);
    }

    private static Dictionary<int, decimal> MovieWeights(
        IReadOnlyCollection<TasteViewing> viewings,
        IReadOnlyCollection<TasteReview> reviews,
        DateTime nowUtc
    )
    {
        var best = new Dictionary<int, decimal>();

        void Offer(int movieId, decimal weight)
        {
            if (!best.TryGetValue(movieId, out var current) || weight > current)
                best[movieId] = weight;
        }

        foreach (var viewing in viewings)
        {
            var date = viewing.CheckedIn ? viewing.CheckedInAt ?? viewing.StartTime : viewing.StartTime;
            var state = viewing.CheckedIn
                ? TasteWeights.CheckedInViewing
                : TasteWeights.UnscannedViewing;
            Offer(viewing.MovieId, state * TasteWeights.Recency(date, nowUtc));
        }

        var ratings = new Dictionary<int, int>();
        foreach (var review in reviews.OrderBy(r => r.CreatedAt))
        {
            ratings[review.MovieId] = review.Rating;
            Offer(
                review.MovieId,
                TasteWeights.CheckedInViewing * TasteWeights.Recency(review.CreatedAt, nowUtc)
            );
        }

        return best.ToDictionary(
            b => b.Key,
            b => ratings.TryGetValue(b.Key, out var rating) ? b.Value * TasteWeights.RatingFactor(rating) : b.Value
        );
    }

    private static void Add(
        Dictionary<(TasteSignalKind Kind, int RefId), decimal> totals,
        TasteSignalKind kind,
        int refId,
        decimal weight
    )
    {
        var key = (kind, refId);
        totals[key] = totals.GetValueOrDefault(key) + weight;
    }
}
