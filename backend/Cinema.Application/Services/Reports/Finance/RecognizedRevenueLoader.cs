using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

internal enum RevenueKind
{
    Ticket,
    Combo,
    GiftCard,
}

internal sealed record RecognizedLine(
    int OrderId,
    SalesChannel Channel,
    DateOnly Date,
    RevenueKind Kind,
    decimal Net,
    decimal TierDiscount,
    decimal PromotionDiscount,
    decimal OtherDiscount,
    int? ComboId,
    string? ComboName,
    int SoldQuantity,
    decimal? UnitCost
);

internal static class RecognizedRevenueLoader
{
    public static async Task<List<RecognizedLine>> LoadAsync(
        IAppDbContext db,
        CinemaSettingsSnapshot settings,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    )
    {
        var fromUtc = BusinessDay.StartUtc(from, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(to, settings.OpeningTime, settings.ClosingTime);

        var showtimeRows = await Project(
                Billable(db.OrderLines.AsNoTracking().ExcludingExchangedOut(db))
                    .Where(l =>
                        l.ShowtimeId != null
                        && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                        && l.Showtime!.StartTime >= fromUtc
                        && l.Showtime.StartTime < toUtc
                        && !db.ShowtimeDailyStats.Any(d =>
                            d.ShowtimeId == l.ShowtimeId && d.Kind != ShowtimeKind.Regular
                        )
                    )
            )
            .ToListAsync(cancellationToken);

        var standaloneRows = await Project(
                Billable(db.OrderLines.AsNoTracking())
                    .Where(l =>
                        l.ShowtimeId == null
                        && (
                            l.LineType == OrderLineType.Combo
                            || l.LineType == OrderLineType.GiftCard
                            || (
                                l.LineType == OrderLineType.Fee
                                && l.Order.Type == OrderType.Supplement
                            )
                        )
                    )
            )
            .Where(r => r.PaidAt >= fromUtc && r.PaidAt < toUtc)
            .ToListAsync(cancellationToken);

        var result = new List<RecognizedLine>();

        foreach (var r in showtimeRows)
        {
            var share = ShowtimeRefundShare(r);
            var kind = r.LineType == OrderLineType.Combo ? RevenueKind.Combo : RevenueKind.Ticket;
            var net = kind == RevenueKind.Combo
                ? Math.Max(0m, r.PaidAmount - share)
                : r.PaidAmount - share;
            result.Add(
                Build(
                    r,
                    BusinessDay.Of(r.StartTime!.Value, settings.OpeningTime, settings.ClosingTime),
                    kind,
                    net
                )
            );
        }

        foreach (var order in standaloneRows.GroupBy(r => r.OrderId))
        {
            var rows = order.OrderBy(r => r.PaidAmount).ThenBy(r => r.OrderLineId).ToList();
            var cancelled = rows[0].OrderStatus == OrderStatus.Cancelled;
            var refunds = cancelled
                ? StandaloneRefundShares(rows)
                : rows.Select(r => Math.Min(r.PaidAmount, r.RefundedAmount)).ToArray();
            var date = BusinessDay.Of(rows[0].PaidAt, settings.OpeningTime, settings.ClosingTime);

            for (var i = 0; i < rows.Count; i++)
            {
                var kind = rows[i].LineType switch
                {
                    OrderLineType.Combo => RevenueKind.Combo,
                    OrderLineType.GiftCard => RevenueKind.GiftCard,
                    _ => RevenueKind.Ticket,
                };
                result.Add(Build(rows[i], date, kind, rows[i].PaidAmount - refunds[i]));
            }
        }

        return result;
    }

    private static IQueryable<OrderLine> Billable(IQueryable<OrderLine> lines) =>
        lines.Where(l =>
            l.Order.Status == OrderStatus.Confirmed
            || (
                l.Order.Status == OrderStatus.Cancelled
                && l.Order.PaymentTransactions.Any(t =>
                    t.Type == PaymentTransactionType.Charge && t.Status == PaymentStatus.Success
                )
            )
        );

    private static IQueryable<LineRow> Project(IQueryable<OrderLine> lines) =>
        lines.Select(l => new LineRow
        {
            OrderId = l.OrderId,
            OrderLineId = l.OrderLineId,
            ComboId = l.ComboId,
            ComboName = l.Combo != null ? l.Combo.Name : null,
            Quantity = l.Quantity,
            RefundedQuantity = l.RefundedQuantity,
            UnitCost = l.UnitCost,
            LineType = l.LineType,
            Channel = l.Order.Channel,
            OrderStatus = l.Order.Status,
            StartTime = l.Showtime != null ? l.Showtime.StartTime : (DateTime?)null,
            PaidAt =
                l.Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                        && t.CompletedAt != null
                    )
                    .Min(t => t.CompletedAt) ?? l.Order.CreatedAt,
            PaidAmount = l.PaidAmount,
            RefundedAmount = l.RefundedAmount,
            Charged =
                l.Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            Refunded =
                l.Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Refund
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            Discounts = l
                .AdjustmentAllocations.Select(a => new DiscountRow
                {
                    Type = a.Adjustment.Type,
                    Amount = a.Amount,
                })
                .ToList(),
        });

    private static RecognizedLine Build(LineRow r, DateOnly date, RevenueKind kind, decimal net) =>
        new(
            r.OrderId,
            r.Channel,
            date,
            kind,
            net,
            r.Discounts.Where(d => d.Type == PriceAdjustmentType.TierDiscount).Sum(d => d.Amount),
            r.Discounts.Where(d => d.Type == PriceAdjustmentType.Promotion).Sum(d => d.Amount),
            r.Discounts
                .Where(d =>
                    d.Type != PriceAdjustmentType.TierDiscount
                    && d.Type != PriceAdjustmentType.Promotion
                )
                .Sum(d => d.Amount),
            r.ComboId,
            r.ComboName,
            Math.Max(0, r.Quantity - r.RefundedQuantity),
            r.UnitCost
        );

    private static decimal ShowtimeRefundShare(LineRow r)
    {
        if (r.OrderStatus == OrderStatus.Confirmed)
            return Math.Min(r.PaidAmount, r.RefundedAmount);

        if (r.Charged <= 0m)
            return 0m;

        var ratio = Math.Min(1m, r.Refunded / r.Charged);
        return Math.Min(r.PaidAmount, OrderPricing.RoundToDong(r.PaidAmount * ratio));
    }

    private static decimal[] StandaloneRefundShares(IReadOnlyList<LineRow> rows)
    {
        var charged = rows[0].Charged;
        if (charged <= 0m)
            return new decimal[rows.Count];

        var paid = rows.Select(r => r.PaidAmount).ToList();
        var ratio = Math.Min(1m, rows[0].Refunded / charged);
        return OrderPricing.AllocateProportionally(
            OrderPricing.RoundToDong(paid.Sum() * ratio),
            paid
        );
    }

    private sealed class DiscountRow
    {
        public PriceAdjustmentType Type { get; init; }
        public decimal Amount { get; init; }
    }

    private sealed class LineRow
    {
        public int OrderId { get; init; }
        public int OrderLineId { get; init; }
        public int? ComboId { get; init; }
        public string? ComboName { get; init; }
        public int Quantity { get; init; }
        public int RefundedQuantity { get; init; }
        public decimal? UnitCost { get; init; }
        public OrderLineType LineType { get; init; }
        public SalesChannel Channel { get; init; }
        public OrderStatus OrderStatus { get; init; }
        public DateTime? StartTime { get; init; }
        public DateTime PaidAt { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RefundedAmount { get; init; }
        public decimal Charged { get; init; }
        public decimal Refunded { get; init; }
        public List<DiscountRow> Discounts { get; init; } = [];
    }
}
