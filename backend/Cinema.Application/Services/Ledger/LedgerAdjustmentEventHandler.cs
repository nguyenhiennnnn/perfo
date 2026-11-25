using Cinema.Application.Caching;
using Cinema.Application.DTOs.Ledger;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Ledger;

public class LedgerAdjustmentEventHandler(
    IAppDbContext db,
    IAppCache cache,
    ILedgerAdjustmentWriter writer
) : IBackgroundEventHandler<OrderRefunded>,
        IBackgroundEventHandler<OrderCancelled>,
        IBackgroundEventHandler<OrderExchanged>
{
    private sealed record Line(
        OrderLineType LineType,
        int? ShowtimeId,
        DateTime? StartTime,
        decimal PaidAmount
    );

    private sealed record Source(string OrderCode, DateTime PaidAt, List<Line> Lines);

    public async Task HandleAsync(OrderRefunded businessEvent, CancellationToken cancellationToken)
    {
        if (businessEvent.Amount <= 0m)
            return;

        var source = await LoadSourceAsync(businessEvent.OrderId, cancellationToken);
        if (source is null || source.Lines.Count == 0)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var paid = source.Lines.Select(l => l.PaidAmount).ToList();
        var total = paid.Sum();
        if (total <= 0m)
            return;

        var shares = OrderPricing.AllocateProportionally(Math.Min(businessEvent.Amount, total), paid);
        var byDate = new Dictionary<DateOnly, Totals>();
        for (var i = 0; i < source.Lines.Count; i++)
        {
            if (shares[i] == 0m)
                continue;

            var line = source.Lines[i];
            var date = OriginalDate(line, source.PaidAt, settings);
            if (!byDate.TryGetValue(date, out var totals))
                byDate[date] = totals = new Totals();

            switch (line.LineType)
            {
                case OrderLineType.Combo:
                    totals.Combo += shares[i];
                    break;
                case OrderLineType.GiftCard:
                    totals.GiftCard += shares[i];
                    break;
                default:
                    totals.Ticket += shares[i];
                    break;
            }

            if (line.ShowtimeId is not null)
                totals.ShowtimeRefund += shares[i];
        }

        foreach (var (date, totals) in byDate.OrderBy(e => e.Key))
            await writer.RecordIfOriginalDayClosedAsync(
                new LedgerAdjustmentInput(
                    date,
                    LedgerAdjustmentType.Refund,
                    string.Format(
                        Messages.Ledger_Reason_RefundAfterClose,
                        source.OrderCode,
                        businessEvent.RefundTransactionId
                    ),
                    TicketRevenueDelta: -totals.Ticket,
                    ComboRevenueDelta: -totals.Combo,
                    GiftCardSoldDelta: -totals.GiftCard,
                    RefundAmountDelta: totals.ShowtimeRefund,
                    OrderId: businessEvent.OrderId,
                    PaymentTransactionId: businessEvent.RefundTransactionId,
                    SourceKey: $"refund:{businessEvent.RefundTransactionId}"
                ),
                cancellationToken
            );
    }

    public async Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken)
    {
        var charged = await db
            .PaymentTransactions.AsNoTracking()
            .AnyAsync(
                t =>
                    t.OrderId == businessEvent.OrderId
                    && t.Type == PaymentTransactionType.Charge
                    && t.Status == PaymentStatus.Success,
                cancellationToken
            );
        if (!charged)
            return;

        var source = await LoadSourceAsync(businessEvent.OrderId, cancellationToken);
        if (source is null)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var showtimeDays = source
            .Lines.Where(l => l.ShowtimeId is not null)
            .GroupBy(l => OriginalDate(l, source.PaidAt, settings))
            .ToDictionary(g => g.Key, g => g.Select(l => l.ShowtimeId).Distinct().Count());

        foreach (var (date, count) in showtimeDays.OrderBy(e => e.Key))
            await writer.RecordIfOriginalDayClosedAsync(
                new LedgerAdjustmentInput(
                    date,
                    LedgerAdjustmentType.Cancellation,
                    string.Format(Messages.Ledger_Reason_CancelAfterClose, source.OrderCode),
                    CancelledOrdersDelta: count,
                    OrderId: businessEvent.OrderId,
                    SourceKey: $"cancel:{businessEvent.OrderId}"
                ),
                cancellationToken
            );
    }

    public async Task HandleAsync(OrderExchanged businessEvent, CancellationToken cancellationToken)
    {
        var rows = await db
            .OrderExchanges.AsNoTracking()
            .Where(e => e.NewOrderLine.OrderId == businessEvent.ExchangeOrderId)
            .Select(e => new
            {
                OldStart = e.OldShowtime.StartTime,
                NewStart = e.NewShowtime.StartTime,
                OldPaid = e.OldOrderLine.PaidAmount,
                OldDiscount = e.OldOrderLine.AdjustmentAllocations.Sum(a => (decimal?)a.Amount) ?? 0m,
                NewPaid = e.NewOrderLine.PaidAmount,
            })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return;

        var orderCode = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.OrderId)
            .Select(o => o.OrderCode)
            .FirstAsync(cancellationToken);
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        var byDate = new Dictionary<DateOnly, ExchangeTotals>();
        foreach (var row in rows)
        {
            var oldDate = BusinessDay.Of(row.OldStart, settings.OpeningTime, settings.ClosingTime);
            var newDate = BusinessDay.Of(row.NewStart, settings.OpeningTime, settings.ClosingTime);
            var oldTotals = byDate.GetValueOrDefault(oldDate) ?? (byDate[oldDate] = new());
            oldTotals.Ticket -= row.OldPaid;
            oldTotals.Discount -= row.OldDiscount;
            var newTotals = byDate.GetValueOrDefault(newDate) ?? (byDate[newDate] = new());
            newTotals.Ticket += row.NewPaid;
        }

        foreach (var (date, totals) in byDate.OrderBy(e => e.Key))
            await writer.RecordIfOriginalDayClosedAsync(
                new LedgerAdjustmentInput(
                    date,
                    LedgerAdjustmentType.ShowtimeChange,
                    string.Format(Messages.Ledger_Reason_ExchangeAfterClose, orderCode),
                    TicketRevenueDelta: totals.Ticket,
                    DiscountDelta: totals.Discount,
                    OrderId: businessEvent.OrderId,
                    SourceKey: $"exchange:{businessEvent.ExchangeOrderId}"
                ),
                cancellationToken
            );
    }

    private async Task<Source?> LoadSourceAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new
            {
                o.OrderCode,
                o.CreatedAt,
                o.Type,
                PaidAt = o
                    .PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                        && t.CompletedAt != null
                    )
                    .Min(t => t.CompletedAt),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;

        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderId == orderId)
            .Where(l =>
                (
                    l.ShowtimeId != null
                    && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                    && !db.ShowtimeDailyStats.Any(d =>
                        d.ShowtimeId == l.ShowtimeId && d.Kind != ShowtimeKind.Regular
                    )
                )
                || (
                    l.ShowtimeId == null
                    && (
                        l.LineType == OrderLineType.Combo
                        || l.LineType == OrderLineType.GiftCard
                        || (l.LineType == OrderLineType.Fee && order.Type == OrderType.Supplement)
                    )
                )
            )
            .OrderBy(l => l.PaidAmount)
            .ThenBy(l => l.OrderLineId)
            .Select(l => new Line(
                l.LineType,
                l.ShowtimeId,
                l.Showtime != null ? l.Showtime.StartTime : (DateTime?)null,
                l.PaidAmount
            ))
            .ToListAsync(cancellationToken);

        return new Source(order.OrderCode, order.PaidAt ?? order.CreatedAt, lines);
    }

    private static DateOnly OriginalDate(Line line, DateTime paidAt, CinemaSettingsSnapshot settings) =>
        BusinessDay.Of(line.StartTime ?? paidAt, settings.OpeningTime, settings.ClosingTime);

    private sealed class ExchangeTotals
    {
        public decimal Ticket { get; set; }
        public decimal Discount { get; set; }
    }

    private sealed class Totals
    {
        public decimal Ticket { get; set; }
        public decimal Combo { get; set; }
        public decimal GiftCard { get; set; }
        public decimal ShowtimeRefund { get; set; }
    }
}
