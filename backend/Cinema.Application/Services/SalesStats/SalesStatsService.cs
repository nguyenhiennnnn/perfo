using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.SalesStats;

public class SalesStatsService(IAppDbContext db, IAppCache cache) : ISalesStatsService
{
    private const int ChunkSize = 200;
    private const int RecentPastDays = 2;
    private const int RecentPaymentDays = 2;

    public async Task RefreshShowtimesAsync(
        IReadOnlyCollection<int> showtimeIds,
        CancellationToken cancellationToken
    )
    {
        if (showtimeIds.Count == 0)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var marks = SalesStatsMarks.For(settings.SalesPaceCustomMarkMinutes);
        foreach (var chunk in showtimeIds.Distinct().Chunk(ChunkSize))
            await RefreshChunkAsync([.. chunk], settings, marks, cancellationToken);
    }

    public async Task RefreshPaymentDaysAsync(
        IReadOnlyCollection<DateOnly> businessDays,
        CancellationToken cancellationToken
    )
    {
        if (businessDays.Count == 0)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        foreach (var day in businessDays.Distinct().Order())
            await RefreshPaymentDayAsync(day, settings, cancellationToken);
    }

    public async Task RefreshForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var showtimeIds = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderId == orderId && l.ShowtimeId != null)
            .Select(l => l.ShowtimeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var completedAt = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == orderId && t.Status == PaymentStatus.Success && t.CompletedAt != null
            )
            .Select(t => t.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var days = completedAt
            .Select(c => BusinessDay.Of(c, settings.OpeningTime, settings.ClosingTime))
            .Distinct()
            .ToList();

        await RefreshShowtimesAsync(showtimeIds, cancellationToken);
        await RefreshPaymentDaysAsync(days, cancellationToken);
    }

    public async Task RefreshRecentAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var from = now.AddDays(-RecentPastDays);
        var to = now.AddMinutes(SalesStatsMarks.LookaheadMinutes(settings.SalesPaceCustomMarkMinutes));

        var showtimeIds = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.StartTime >= from && s.StartTime <= to)
            .Select(s => s.ShowtimeId)
            .ToListAsync(cancellationToken);

        var today = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        var days = Enumerable.Range(0, RecentPaymentDays).Select(i => today.AddDays(-i)).ToList();

        await RefreshShowtimesAsync(showtimeIds, cancellationToken);
        await RefreshPaymentDaysAsync(days, cancellationToken);
    }

    public async Task<int> RebuildDayAsync(DateOnly businessDay, CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var from = BusinessDay.StartUtc(businessDay, settings.OpeningTime, settings.ClosingTime);
        var to = BusinessDay.EndUtc(businessDay, settings.OpeningTime, settings.ClosingTime);

        var showtimeIds = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.StartTime >= from && s.StartTime < to)
            .Select(s => s.ShowtimeId)
            .ToListAsync(cancellationToken);

        var misplacedIds = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(s => s.BusinessDate == businessDay && !showtimeIds.Contains(s.ShowtimeId))
            .Select(s => s.ShowtimeId)
            .ToListAsync(cancellationToken);

        await RefreshShowtimesAsync([.. showtimeIds, .. misplacedIds], cancellationToken);
        await RefreshPaymentDaysAsync([businessDay], cancellationToken);

        return showtimeIds.Count;
    }

    private async Task RefreshChunkAsync(
        List<int> ids,
        CinemaSettingsSnapshot settings,
        IReadOnlyList<int> marks,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var showtimes = await db
            .Showtimes.AsNoTracking()
            .Where(s => ids.Contains(s.ShowtimeId))
            .Select(s => new { s.ShowtimeId, s.StartTime, s.Kind })
            .ToListAsync(cancellationToken);
        if (showtimes.Count == 0)
            return;

        var found = showtimes.Select(s => s.ShowtimeId).ToList();
        var sellableByShowtime = await db
            .ShowtimeSeats.AsNoTracking()
            .WhereSellable()
            .Where(ss => found.Contains(ss.ShowtimeId))
            .GroupBy(ss => ss.ShowtimeId)
            .Select(g => new { ShowtimeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShowtimeId, x => x.Count, cancellationToken);
        var linesByShowtime = (await LoadLinesAsync(found, cancellationToken)).ToLookup(l =>
            l.ShowtimeId
        );
        var rentalRevenueByShowtime = await db
            .RoomRentalRevenueEntries.AsNoTracking()
            .Where(e => found.Contains(e.ShowtimeId))
            .GroupBy(e => e.ShowtimeId)
            .Select(g => new { ShowtimeId = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.ShowtimeId, x => x.Amount, cancellationToken);
        var existingPaces = await db
            .ShowtimeSalesPaces.AsNoTracking()
            .Where(p => found.Contains(p.ShowtimeId))
            .Select(p => new { p.ShowtimeId, p.MarkMinutesBeforeStart })
            .ToListAsync(cancellationToken);

        foreach (var showtime in showtimes)
        {
            var lines = linesByShowtime[showtime.ShowtimeId].ToList();
            var sellable = sellableByShowtime.GetValueOrDefault(showtime.ShowtimeId);
            var date = BusinessDay.Of(showtime.StartTime, settings.OpeningTime, settings.ClosingTime);

            await UpsertShowtimeStatAsync(
                showtime.ShowtimeId,
                date,
                showtime.Kind,
                sellable,
                AddRentalRevenue(
                    Summarize(lines),
                    showtime.Kind,
                    rentalRevenueByShowtime.GetValueOrDefault(showtime.ShowtimeId)
                ),
                now,
                cancellationToken
            );

            var kept = new List<int>();
            foreach (var mark in marks)
            {
                var markTime = showtime.StartTime.AddMinutes(-mark);
                if (now < markTime)
                    continue;

                kept.Add(mark);
                await UpsertPaceAsync(
                    showtime.ShowtimeId,
                    mark,
                    SoldAtMark(lines, markTime),
                    sellable,
                    now,
                    cancellationToken
                );
            }

            foreach (
                var stale in existingPaces.Where(p =>
                    p.ShowtimeId == showtime.ShowtimeId
                    && !kept.Contains(p.MarkMinutesBeforeStart)
                )
            )
            {
                var staleShowtimeId = stale.ShowtimeId;
                var staleMark = stale.MarkMinutesBeforeStart;
                await db
                    .ShowtimeSalesPaces.Where(p =>
                        p.ShowtimeId == staleShowtimeId && p.MarkMinutesBeforeStart == staleMark
                    )
                    .ExecuteDeleteAsync(cancellationToken);
            }
        }
    }

    private async Task RefreshPaymentDayAsync(
        DateOnly day,
        CinemaSettingsSnapshot settings,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var from = BusinessDay.StartUtc(day, settings.OpeningTime, settings.ClosingTime);
        var to = BusinessDay.EndUtc(day, settings.OpeningTime, settings.ClosingTime);

        var rows = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.Status == PaymentStatus.Success
                && t.CompletedAt != null
                && t.CompletedAt >= from
                && t.CompletedAt < to
            )
            .GroupBy(t => new { t.Order.Channel, t.Method })
            .Select(g => new
            {
                g.Key.Channel,
                g.Key.Method,
                Count = g.Count(),
                Collected = g.Where(t => t.Type == PaymentTransactionType.Charge)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                Refunded = g.Where(t => t.Type == PaymentTransactionType.Refund)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                Fee = g.Where(t => t.Type == PaymentTransactionType.Charge)
                    .Sum(t => (decimal?)t.GatewayFee) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
            await UpsertPaymentStatAsync(
                day,
                row.Channel,
                row.Method,
                row.Count,
                row.Collected,
                row.Refunded,
                row.Fee,
                now,
                cancellationToken
            );

        var existing = await db
            .PaymentDailyStats.AsNoTracking()
            .Where(p => p.BusinessDate == day)
            .Select(p => new { p.Channel, p.Method })
            .ToListAsync(cancellationToken);
        foreach (
            var stale in existing.Where(e =>
                !rows.Any(r => r.Channel == e.Channel && r.Method == e.Method)
            )
        )
        {
            var staleChannel = stale.Channel;
            var staleMethod = stale.Method;
            await db
                .PaymentDailyStats.Where(p =>
                    p.BusinessDate == day && p.Channel == staleChannel && p.Method == staleMethod
                )
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private async Task<List<SalesLine>> LoadLinesAsync(
        IReadOnlyCollection<int> showtimeIds,
        CancellationToken cancellationToken
    ) =>
        await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.ShowtimeId != null
                && showtimeIds.Contains(l.ShowtimeId.Value)
                && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                && (
                    l.Order.Status == OrderStatus.Confirmed
                    || (
                        l.Order.Status == OrderStatus.Cancelled
                        && l.Order.PaymentTransactions.Any(t =>
                            t.Type == PaymentTransactionType.Charge
                            && t.Status == PaymentStatus.Success
                        )
                    )
                )
            )
            .Select(l => new SalesLine
            {
                ShowtimeId = l.ShowtimeId!.Value,
                OrderId = l.OrderId,
                LineType = l.LineType,
                OrderStatus = l.Order.Status,
                IsExchangeOrder = l.Order.Type == OrderType.Exchange,
                OrderCreatedAt = l.Order.CreatedAt,
                OrderCancelledAt = l.Order.CancelledAt,
                Quantity = l.Quantity,
                RefundedQuantity = l.RefundedQuantity,
                LockedUnitPrice = l.LockedUnitPrice,
                PaidAmount = l.PaidAmount,
                RefundedAmount = l.RefundedAmount,
                Discount = l.AdjustmentAllocations.Sum(a => (decimal?)a.Amount) ?? 0m,
                FirstPaidAt = l
                    .Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                        && t.CompletedAt != null
                    )
                    .Min(t => t.CompletedAt),
                OrderCharged =
                    l.Order.PaymentTransactions.Where(t =>
                            t.Type == PaymentTransactionType.Charge
                            && t.Status == PaymentStatus.Success
                        )
                        .Sum(t => (decimal?)t.Amount) ?? 0m,
                OrderRefunded =
                    l.Order.PaymentTransactions.Where(t =>
                            t.Type == PaymentTransactionType.Refund
                            && t.Status == PaymentStatus.Success
                        )
                        .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .ToListAsync(cancellationToken);

    private static ShowtimeTotals Summarize(IReadOnlyList<SalesLine> lines)
    {
        var tickets = lines.Where(l => l.LineType == OrderLineType.Ticket).ToList();
        var combos = lines.Where(l => l.LineType == OrderLineType.Combo).ToList();

        return new ShowtimeTotals(
            tickets
                .Where(l => l.OrderStatus == OrderStatus.Confirmed)
                .Sum(l => Math.Max(0, l.Quantity - l.RefundedQuantity)),
            tickets.Sum(l => l.LockedUnitPrice * l.Quantity),
            tickets.Sum(l => l.Discount),
            tickets.Sum(l => l.RefundShare()),
            combos.Sum(l => Math.Max(0m, l.PaidAmount - l.RefundShare())),
            lines
                .Where(l => l.OrderStatus == OrderStatus.Cancelled && !l.IsExchangeOrder)
                .Select(l => l.OrderId)
                .Distinct()
                .Count()
        );
    }

    private static ShowtimeTotals AddRentalRevenue(
        ShowtimeTotals totals,
        ShowtimeKind kind,
        decimal rentalRevenue
    ) =>
        kind == ShowtimeKind.Private && rentalRevenue > 0m
            ? totals with { GrossTicketRevenue = totals.GrossTicketRevenue + rentalRevenue }
            : totals;

    private static int SoldAtMark(IEnumerable<SalesLine> lines, DateTime markTime) =>
        lines
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && (l.FirstPaidAt ?? l.OrderCreatedAt) <= markTime
                && (
                    l.OrderStatus == OrderStatus.Confirmed
                    || (l.OrderCancelledAt is { } cancelledAt && cancelledAt > markTime)
                )
            )
            .Sum(l => l.Quantity);

    private Task UpsertShowtimeStatAsync(
        int showtimeId,
        DateOnly date,
        ShowtimeKind showtimeKind,
        int sellable,
        ShowtimeTotals totals,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var kind = showtimeKind.ToString();
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ShowtimeDailyStats" AS t
                ("ShowtimeId", "BusinessDate", "Kind", "SellableSeats", "TicketsSold",
                 "GrossTicketRevenue", "DiscountAmount", "RefundAmount", "ComboRevenue",
                 "CancelledOrders", "UpdatedAt")
            VALUES
                ({showtimeId}, {date}, {kind}, {sellable}, {totals.TicketsSold},
                 {totals.GrossTicketRevenue}, {totals.DiscountAmount}, {totals.RefundAmount},
                 {totals.ComboRevenue}, {totals.CancelledOrders}, {now})
            ON CONFLICT ("ShowtimeId") DO UPDATE SET
                "BusinessDate" = EXCLUDED."BusinessDate",
                "Kind" = EXCLUDED."Kind",
                "SellableSeats" = EXCLUDED."SellableSeats",
                "TicketsSold" = EXCLUDED."TicketsSold",
                "GrossTicketRevenue" = EXCLUDED."GrossTicketRevenue",
                "DiscountAmount" = EXCLUDED."DiscountAmount",
                "RefundAmount" = EXCLUDED."RefundAmount",
                "ComboRevenue" = EXCLUDED."ComboRevenue",
                "CancelledOrders" = EXCLUDED."CancelledOrders",
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            WHERE (t."BusinessDate", t."Kind", t."SellableSeats", t."TicketsSold",
                   t."GrossTicketRevenue", t."DiscountAmount", t."RefundAmount",
                   t."ComboRevenue", t."CancelledOrders")
                IS DISTINCT FROM
                  (EXCLUDED."BusinessDate", EXCLUDED."Kind", EXCLUDED."SellableSeats",
                   EXCLUDED."TicketsSold", EXCLUDED."GrossTicketRevenue",
                   EXCLUDED."DiscountAmount", EXCLUDED."RefundAmount",
                   EXCLUDED."ComboRevenue", EXCLUDED."CancelledOrders")
            """,
            cancellationToken
        );
    }

    private Task UpsertPaceAsync(
        int showtimeId,
        int mark,
        int ticketsSold,
        int sellable,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var percent =
            sellable > 0
                ? Math.Min(
                    100m,
                    Math.Round(ticketsSold * 100m / sellable, 2, MidpointRounding.AwayFromZero)
                )
                : 0m;
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ShowtimeSalesPaces" AS t
                ("ShowtimeId", "MarkMinutesBeforeStart", "TicketsSold", "SellableSeats",
                 "SoldPercent", "UpdatedAt")
            VALUES ({showtimeId}, {mark}, {ticketsSold}, {sellable}, {percent}, {now})
            ON CONFLICT ("ShowtimeId", "MarkMinutesBeforeStart") DO UPDATE SET
                "TicketsSold" = EXCLUDED."TicketsSold",
                "SellableSeats" = EXCLUDED."SellableSeats",
                "SoldPercent" = EXCLUDED."SoldPercent",
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            WHERE (t."TicketsSold", t."SellableSeats", t."SoldPercent")
                IS DISTINCT FROM
                  (EXCLUDED."TicketsSold", EXCLUDED."SellableSeats", EXCLUDED."SoldPercent")
            """,
            cancellationToken
        );
    }

    private Task UpsertPaymentStatAsync(
        DateOnly day,
        SalesChannel channel,
        PaymentMethod method,
        int count,
        decimal collected,
        decimal refunded,
        decimal fee,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var channelName = channel.ToString();
        var methodName = method.ToString();
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "PaymentDailyStats" AS t
                ("BusinessDate", "Channel", "Method", "TransactionCount", "CollectedAmount",
                 "RefundedAmount", "GatewayFee", "UpdatedAt")
            VALUES ({day}, {channelName}, {methodName}, {count}, {collected}, {refunded},
                    {fee}, {now})
            ON CONFLICT ("BusinessDate", "Channel", "Method") DO UPDATE SET
                "TransactionCount" = EXCLUDED."TransactionCount",
                "CollectedAmount" = EXCLUDED."CollectedAmount",
                "RefundedAmount" = EXCLUDED."RefundedAmount",
                "GatewayFee" = EXCLUDED."GatewayFee",
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            WHERE (t."TransactionCount", t."CollectedAmount", t."RefundedAmount", t."GatewayFee")
                IS DISTINCT FROM
                  (EXCLUDED."TransactionCount", EXCLUDED."CollectedAmount",
                   EXCLUDED."RefundedAmount", EXCLUDED."GatewayFee")
            """,
            cancellationToken
        );
    }

    private sealed record ShowtimeTotals(
        int TicketsSold,
        decimal GrossTicketRevenue,
        decimal DiscountAmount,
        decimal RefundAmount,
        decimal ComboRevenue,
        int CancelledOrders
    );

    private sealed class SalesLine
    {
        public int ShowtimeId { get; init; }
        public int OrderId { get; init; }
        public OrderLineType LineType { get; init; }
        public OrderStatus OrderStatus { get; init; }
        public bool IsExchangeOrder { get; init; }
        public DateTime OrderCreatedAt { get; init; }
        public DateTime? OrderCancelledAt { get; init; }
        public DateTime? FirstPaidAt { get; init; }
        public int Quantity { get; init; }
        public int RefundedQuantity { get; init; }
        public decimal LockedUnitPrice { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RefundedAmount { get; init; }
        public decimal Discount { get; init; }
        public decimal OrderCharged { get; init; }
        public decimal OrderRefunded { get; init; }

        public decimal RefundShare()
        {
            if (OrderStatus == OrderStatus.Confirmed)
                return Math.Min(PaidAmount, RefundedAmount);

            if (OrderCharged <= 0m)
                return 0m;

            var ratio = Math.Min(1m, OrderRefunded / OrderCharged);
            return Math.Min(PaidAmount, OrderPricing.RoundToDong(PaidAmount * ratio));
        }
    }
}
