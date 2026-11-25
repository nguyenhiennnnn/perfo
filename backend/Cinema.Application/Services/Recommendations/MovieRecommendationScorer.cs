using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Recommendations;

internal enum RecommendationReasonType
{
    Popular,
    Director,
    Genre,
    Actor,
    SimilarMovie,
}

internal sealed record CandidateMovie(
    int MovieId,
    int? DirectorId,
    IReadOnlyList<int> GenreIds,
    IReadOnlyList<int> ActorIds,
    bool HasAvailableSeats,
    decimal PopularityScore,
    decimal AverageRating
);

internal sealed record SimilarityHit(int SourceMovieId, decimal Score);

internal sealed record ScoredMovie(
    int MovieId,
    decimal Score,
    decimal AverageRating,
    RecommendationReasonType ReasonType,
    int RefId
);

internal sealed class TasteLookup
{
    public static readonly TasteLookup Empty = new([]);

    private readonly Dictionary<(TasteSignalKind Kind, int RefId), decimal> weights;
    private readonly Dictionary<TasteSignalKind, decimal> maximums;

    public TasteLookup(IEnumerable<CustomerTasteSignal> signals)
    {
        weights = [];
        maximums = [];
        foreach (var signal in signals)
        {
            weights[(signal.Kind, signal.RefId)] = signal.Weight;
            maximums[signal.Kind] = Math.Max(maximums.GetValueOrDefault(signal.Kind), signal.Weight);
        }
    }

    public bool HasSignals => weights.Count > 0;

    public decimal Normalized(TasteSignalKind kind, int refId)
    {
        var max = maximums.GetValueOrDefault(kind);
        return max > 0m && weights.TryGetValue((kind, refId), out var weight) ? weight / max : 0m;
    }
}

internal static class MovieRecommendationScorer
{
    public static ScoredMovie Score(
        CandidateMovie movie,
        TasteLookup taste,
        SimilarityHit? similarity
    )
    {
        var (genre, genreId) = Best(movie.GenreIds, id => taste.Normalized(TasteSignalKind.Genre, id));
        var (actor, actorId) = Best(movie.ActorIds, id => taste.Normalized(TasteSignalKind.Actor, id));
        var director =
            movie.DirectorId is { } directorId
                ? taste.Normalized(TasteSignalKind.Director, directorId)
                : 0m;

        var directorPart = RecommendationWeights.Taste * RecommendationWeights.DirectorShare * director;
        var genrePart = RecommendationWeights.Taste * RecommendationWeights.GenreShare * genre;
        var actorPart = RecommendationWeights.Taste * RecommendationWeights.ActorShare * actor;
        var similarPart = RecommendationWeights.Similarity * (similarity?.Score ?? 0m);
        var popularPart = RecommendationWeights.Popularity * movie.PopularityScore;

        var total = directorPart + genrePart + actorPart + similarPart + popularPart;
        if (!movie.HasAvailableSeats)
            total *= RecommendationWeights.SoldOutFactor;

        var (reason, refId) = (RecommendationReasonType.Popular, 0);
        var best = 0m;
        void Consider(decimal part, RecommendationReasonType type, int id)
        {
            if (part > best)
            {
                best = part;
                (reason, refId) = (type, id);
            }
        }

        Consider(directorPart, RecommendationReasonType.Director, movie.DirectorId ?? 0);
        Consider(genrePart, RecommendationReasonType.Genre, genreId);
        Consider(similarPart, RecommendationReasonType.SimilarMovie, similarity?.SourceMovieId ?? 0);
        Consider(actorPart, RecommendationReasonType.Actor, actorId);

        return new ScoredMovie(movie.MovieId, Math.Round(total, 6), movie.AverageRating, reason, refId);
    }

    public static IEnumerable<ScoredMovie> Rank(IEnumerable<ScoredMovie> scored) =>
        scored
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.AverageRating)
            .ThenBy(s => s.MovieId);

    private static (decimal Value, int Id) Best(IReadOnlyList<int> ids, Func<int, decimal> normalize)
    {
        decimal best = 0m;
        var bestId = 0;
        foreach (var id in ids.Distinct())
        {
            var value = normalize(id);
            if (value > best)
            {
                best = value;
                bestId = id;
            }
        }
        return (best, bestId);
    }
}
