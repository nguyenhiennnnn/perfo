namespace Cinema.Application.Services.Recommendations;

public static class MovieAffinityWeights
{
    public const int MinCoViewers = 2;
    public const int MaxSimilarPerMovie = 20;
    public const int PopularityWindowDays = 90;
    public const double PopularityHalfLifeDays = 30d;
    public const decimal VolumeShare = 0.6m;
    public const decimal RatingShare = 0.4m;
    public const int RatingPriorCount = 5;
    public const decimal RatingPriorMean = 3m;
    public const decimal MaxRating = 5m;

    public static decimal Recency(DateTime eventUtc, DateTime nowUtc)
    {
        var ageDays = Math.Max((nowUtc - eventUtc).TotalDays, 0d);
        return (decimal)Math.Pow(0.5d, ageDays / PopularityHalfLifeDays);
    }
}
