using Cinema.Application.Caching;

namespace Cinema.Application.Services.Bundles;

public sealed record BundleRetailInput(
    IReadOnlyList<BundleItemSnapshot> Items,
    IReadOnlyList<int> MovieIds,
    IReadOnlyList<int> ShowtimeIds,
    IReadOnlyList<int> DaysOfWeek,
    IReadOnlyList<int> SeatTypeIds,
    TimeOnly? AllowedFromTime,
    TimeOnly? AllowedToTime
)
{
    public static BundleRetailInput From(BundleSnapshot b) =>
        new(
            b.Items,
            b.MovieIds,
            b.ShowtimeIds,
            b.DaysOfWeek,
            b.SeatTypeIds,
            b.AllowedFromTime,
            b.AllowedToTime
        );
}
