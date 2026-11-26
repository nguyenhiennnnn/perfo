using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Bundles;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public class OrderBundleComposer(
    IAppDbContext db,
    IAppCache cache,
    IBundleAvailabilityService availability
) : IOrderBundleComposer
{
    public async Task ComposeAsync(
        Order order,
        int showtimeId,
        IReadOnlyList<BundleSelectionRequest>? selections,
        CancellationToken cancellationToken
    )
    {
        if (selections is null || selections.Count == 0)
            return;

        var applicable = (await availability.GetForShowtimeAsync(showtimeId, cancellationToken))
            .ToDictionary(b => b.BundleId);
        var snapshots = (await cache.GetAsync(CacheKeys.Bundles, cancellationToken)).ToDictionary(
            b => b.BundleId
        );

        foreach (var selection in selections)
        {
            if (
                !applicable.TryGetValue(selection.BundleId, out var offer)
                || !snapshots.ContainsKey(selection.BundleId)
            )
                throw new BusinessRuleException(Messages.Rule_BundleNotApplicable);
            if (offer.RemainingToday is { } remaining && remaining < selection.Quantity)
                throw new ConflictAppException(Messages.Conflict_BundleDailyLimitReached);
        }

        var ticketLines = order
            .Lines.Where(l => l.LineType == OrderLineType.Ticket && l.TicketTypeId != null)
            .OrderBy(l => l.ShowtimeSeatId)
            .ToList();
        var seatIds = ticketLines.Select(l => l.ShowtimeSeatId!.Value).ToList();
        var seatTypes = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => seatIds.Contains(ss.ShowtimeSeatId))
            .Select(ss => new { ss.ShowtimeSeatId, ss.Seat.SeatTypeId })
            .ToDictionaryAsync(x => x.ShowtimeSeatId, x => x.SeatTypeId, cancellationToken);
        var pool = ticketLines
            .GroupBy(l => l.TicketTypeId!.Value)
            .ToDictionary(g => g.Key, g => new Queue<OrderLine>(g));

        var comboIds = selections
            .SelectMany(s => snapshots[s.BundleId].Items)
            .Where(i => i.ComboId is not null)
            .Select(i => i.ComboId!.Value)
            .Distinct()
            .ToList();
        var comboPrices = comboIds.Count == 0
            ? []
            : await db
                .Combos.AsNoTracking()
                .Where(c => comboIds.Contains(c.ComboId) && c.IsActive)
                .ToDictionaryAsync(c => c.ComboId, c => c.Price, cancellationToken);

        foreach (var selection in selections)
        {
            var bundle = snapshots[selection.BundleId];
            var packagePrice = OrderPricing.RoundToDong(bundle.Price);
            for (var unit = 0; unit < selection.Quantity; unit++)
            {
                var unitLines = new List<OrderLine>();
                foreach (var item in bundle.Items)
                {
                    if (item.TicketTypeId is { } ticketTypeId)
                    {
                        if (
                            !pool.TryGetValue(ticketTypeId, out var queue)
                            || queue.Count < item.Quantity
                        )
                            throw new BusinessRuleException(Messages.Rule_BundleSeatsMismatch);

                        for (var i = 0; i < item.Quantity; i++)
                        {
                            var line = queue.Dequeue();
                            if (
                                bundle.SeatTypeIds.Count > 0
                                && (
                                    seatTypes.GetValueOrDefault(line.ShowtimeSeatId!.Value)
                                        is not { } seatTypeId
                                    || !bundle.SeatTypeIds.Contains(seatTypeId)
                                )
                            )
                                throw new BusinessRuleException(Messages.Rule_BundleSeatsMismatch);

                            unitLines.Add(line);
                        }

                        continue;
                    }

                    if (!comboPrices.TryGetValue(item.ComboId!.Value, out var comboPrice))
                        throw new BusinessRuleException(Messages.Rule_BundleNotApplicable);

                    var comboLine = new OrderLine
                    {
                        LineType = OrderLineType.Combo,
                        ShowtimeId = showtimeId,
                        ComboId = item.ComboId,
                        Quantity = item.Quantity,
                        UnitListPrice = comboPrice,
                        LockedUnitPrice = comboPrice,
                    };
                    order.Lines.Add(comboLine);
                    unitLines.Add(comboLine);
                }

                var bases = unitLines.Select(l => l.LockedUnitPrice * l.Quantity).ToList();
                var discount = bases.Sum() - packagePrice;
                if (discount <= 0m)
                    throw new BusinessRuleException(Messages.Rule_BundleNotApplicable);

                order.PriceAdjustments.Add(
                    OrderPricing.BuildAdjustment(
                        PriceAdjustmentType.PackagePrice,
                        nameof(Bundle),
                        bundle.BundleId,
                        unitLines,
                        DiscountAllocator.Allocate(discount, bases)
                    )
                );
            }
        }
    }
}
