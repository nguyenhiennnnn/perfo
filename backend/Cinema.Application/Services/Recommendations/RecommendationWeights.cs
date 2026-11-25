namespace Cinema.Application.Services.Recommendations;

public static class RecommendationWeights
{
    public const decimal Taste = 0.45m;
    public const decimal Similarity = 0.30m;
    public const decimal Popularity = 0.25m;

    public const decimal GenreShare = 0.5m;
    public const decimal ActorShare = 0.2m;
    public const decimal DirectorShare = 0.3m;

    public const decimal SoldOutFactor = 0.3m;

    public const decimal SlotShare = 0.4m;
    public const decimal ScreenShare = 0.3m;
    public const decimal PriceShare = 0.3m;

    public const int DefaultLimit = 10;
    public const int MaxLimit = 20;
    public const int ShowtimesPerMovie = 3;
}
