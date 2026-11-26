namespace Cinema.Domain.Entities;

public class BundleDailyUsage
{
    public int BundleId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public int UsedCount { get; set; }

    public Bundle Bundle { get; set; } = null!;
}
