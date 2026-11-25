namespace Cinema.Application.Services.Recommendations;

public static class TasteWeights
{
    public const decimal CheckedInViewing = 1m;
    public const decimal UnscannedViewing = 0.3m;
    public const double RecencyHalfLifeDays = 180d;
    public const decimal MinimumWeight = 0.0001m;

    public static decimal Recency(DateTime eventUtc, DateTime nowUtc)
    {
        var ageDays = Math.Max((nowUtc - eventUtc).TotalDays, 0d);
        return Math.Round((decimal)Math.Pow(0.5d, ageDays / RecencyHalfLifeDays), 6);
    }

    public static decimal RatingFactor(int rating) =>
        rating switch
        {
            <= 1 => 0.25m,
            2 => 0.5m,
            3 => 0.75m,
            4 => 1.25m,
            _ => 1.5m,
        };
}
