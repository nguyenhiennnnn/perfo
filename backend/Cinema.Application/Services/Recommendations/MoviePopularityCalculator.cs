namespace Cinema.Application.Services.Recommendations;

public sealed record PopularityTicket(int MovieId, DateTime At, int Tickets);

public sealed record PopularityReviews(int Count, int RatingSum);

public sealed record MoviePopularityResult(
    int MovieId,
    int RecentTickets,
    int ReviewCount,
    decimal AverageRating,
    decimal Score
);

public static class MoviePopularityCalculator
{
    public static List<MoviePopularityResult> Calculate(
        IReadOnlyCollection<int> movieIds,
        IReadOnlyCollection<PopularityTicket> tickets,
        IReadOnlyDictionary<int, PopularityReviews> reviews,
        DateTime now
    )
    {
        var ticketsByMovie = tickets
            .GroupBy(t => t.MovieId)
            .ToDictionary(
                g => g.Key,
                g => (Raw: g.Sum(t => t.Tickets), Weighted: g.Sum(t => t.Tickets * MovieAffinityWeights.Recency(t.At, now)))
            );

        var maxWeighted = movieIds
            .Select(id => ticketsByMovie.TryGetValue(id, out var v) ? v.Weighted : 0m)
            .DefaultIfEmpty(0m)
            .Max();

        var totalCount = reviews.Values.Sum(r => r.Count);
        var globalMean =
            totalCount > 0
                ? (decimal)reviews.Values.Sum(r => r.RatingSum) / totalCount
                : MovieAffinityWeights.RatingPriorMean;

        return movieIds
            .Distinct()
            .Order()
            .Select(id =>
            {
                var (raw, weighted) = ticketsByMovie.TryGetValue(id, out var v) ? v : (0, 0m);
                var review = reviews.GetValueOrDefault(id) ?? new PopularityReviews(0, 0);

                var bayesRating =
                    (review.RatingSum + MovieAffinityWeights.RatingPriorCount * globalMean)
                    / (review.Count + MovieAffinityWeights.RatingPriorCount);
                var ratingPart = Math.Clamp(bayesRating / MovieAffinityWeights.MaxRating, 0m, 1m);
                var volumePart = maxWeighted > 0m ? weighted / maxWeighted : 0m;

                var score = Math.Round(
                    volumePart * MovieAffinityWeights.VolumeShare
                        + ratingPart * MovieAffinityWeights.RatingShare,
                    6
                );
                var average =
                    review.Count > 0
                        ? Math.Round((decimal)review.RatingSum / review.Count, 2)
                        : 0m;

                return new MoviePopularityResult(
                    id,
                    raw,
                    review.Count,
                    average,
                    Math.Min(score, 1m)
                );
            })
            .ToList();
    }
}
