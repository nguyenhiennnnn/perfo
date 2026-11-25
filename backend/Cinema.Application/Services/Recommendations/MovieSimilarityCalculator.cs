namespace Cinema.Application.Services.Recommendations;

public sealed record MoviePairViewers(int MovieA, int MovieB, int CoViewers);

public sealed record MovieSimilarityResult(
    int MovieId,
    int SimilarMovieId,
    decimal Score,
    int CoViewers
);

public static class MovieSimilarityCalculator
{
    public static List<MovieSimilarityResult> Calculate(
        IReadOnlyDictionary<int, int> viewersByMovie,
        IEnumerable<MoviePairViewers> pairs,
        IReadOnlySet<int> recommendableMovieIds
    )
    {
        var byMovie = new Dictionary<int, List<MovieSimilarityResult>>();

        foreach (var pair in pairs)
        {
            if (pair.MovieA == pair.MovieB || pair.CoViewers < MovieAffinityWeights.MinCoViewers)
                continue;
            if (
                !viewersByMovie.TryGetValue(pair.MovieA, out var viewersA)
                || !viewersByMovie.TryGetValue(pair.MovieB, out var viewersB)
                || viewersA <= 0
                || viewersB <= 0
            )
                continue;

            var score = Math.Round(
                (decimal)(pair.CoViewers / Math.Sqrt((double)viewersA * viewersB)),
                6
            );
            score = Math.Min(score, 1m);

            if (recommendableMovieIds.Contains(pair.MovieB))
                Add(byMovie, new MovieSimilarityResult(pair.MovieA, pair.MovieB, score, pair.CoViewers));
            if (recommendableMovieIds.Contains(pair.MovieA))
                Add(byMovie, new MovieSimilarityResult(pair.MovieB, pair.MovieA, score, pair.CoViewers));
        }

        return byMovie
            .OrderBy(x => x.Key)
            .SelectMany(x =>
                x.Value.OrderByDescending(r => r.Score)
                    .ThenByDescending(r => r.CoViewers)
                    .ThenBy(r => r.SimilarMovieId)
                    .Take(MovieAffinityWeights.MaxSimilarPerMovie)
            )
            .ToList();
    }

    private static void Add(
        Dictionary<int, List<MovieSimilarityResult>> byMovie,
        MovieSimilarityResult result
    )
    {
        if (!byMovie.TryGetValue(result.MovieId, out var list))
        {
            list = [];
            byMovie[result.MovieId] = list;
        }
        list.Add(result);
    }
}
