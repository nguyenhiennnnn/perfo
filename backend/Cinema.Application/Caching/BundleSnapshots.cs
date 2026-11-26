using Cinema.Domain.Entities;

namespace Cinema.Application.Caching;

public sealed record BundleItemSnapshot(int? TicketTypeId, int? ComboId, int Quantity);

public sealed record BundleSnapshot(
    int BundleId,
    string Name,
    string? Description,
    decimal Price,
    IReadOnlyList<BundleItemSnapshot> Items,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyList<int> MovieIds,
    IReadOnlyList<int> ShowtimeIds,
    IReadOnlyList<int> DaysOfWeek,
    IReadOnlyList<int> SeatTypeIds,
    TimeOnly? AllowedFromTime,
    TimeOnly? AllowedToTime,
    int? DailyLimit,
    bool ExcludesPromotions
)
{
    public static BundleSnapshot From(Bundle b) =>
        new(
            b.BundleId,
            b.Name,
            b.Description,
            b.Price,
            [
                .. b
                    .Items.OrderBy(i => i.BundleItemId)
                    .Select(i => new BundleItemSnapshot(i.TicketTypeId, i.ComboId, i.Quantity)),
            ],
            b.ValidFrom,
            b.ValidTo,
            [.. b.MovieIds],
            [.. b.ShowtimeIds],
            [.. b.DaysOfWeek],
            [.. b.SeatTypeIds],
            b.AllowedFromTime,
            b.AllowedToTime,
            b.DailyLimit,
            b.ExcludesPromotions
        );
}
