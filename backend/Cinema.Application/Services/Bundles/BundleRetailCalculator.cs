using Cinema.Application.Caching;
using Cinema.Application.DTOs.Pricing;

namespace Cinema.Application.Services.Bundles;

public static class BundleRetailCalculator
{
    public static bool InTimeWindow(TimeOnly localTime, TimeOnly? from, TimeOnly? to) =>
        from is not { } start || to is not { } end || (localTime >= start && localTime < end);

    public static decimal? TicketTotal(
        ShowtimePriceTableDto table,
        IReadOnlyList<BundleItemSnapshot> ticketItems,
        IReadOnlyList<int> seatTypeIds
    )
    {
        var rows = table
            .Rows.Where(r =>
                seatTypeIds.Count == 0 || (r.SeatTypeId is { } id && seatTypeIds.Contains(id))
            )
            .ToList();
        if (rows.Count == 0)
            return null;

        var total = 0m;
        foreach (var item in ticketItems)
        {
            var prices = rows.Select(r =>
                    r.Prices.FirstOrDefault(p => p.TicketTypeId == item.TicketTypeId)
                )
                .Where(p => p is { IsEligible: true })
                .Select(p => p!.ListPrice)
                .ToList();
            if (prices.Count == 0)
                return null;

            total += prices.Min() * item.Quantity;
        }

        return total;
    }
}
