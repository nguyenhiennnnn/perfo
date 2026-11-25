using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports;

public class ReportService(IAppDbContext db, IAppCache cache) : IReportService
{
    private static readonly string[] ValidGroupBy = ["day", "week", "month"];

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string groupBy,
        CancellationToken cancellationToken
    )
    {
        if (!ValidGroupBy.Contains(groupBy))
            throw new ValidationAppException(
                Messages.Invalid_GroupBy,
                new Dictionary<string, string[]> { ["groupBy"] = [Messages.Invalid_GroupBy] }
            );

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(fromDate, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(toDate, settings.OpeningTime, settings.ClosingTime);

        var days = new Dictionary<DateOnly, RevenueDay>();
        RevenueDay DayOf(DateOnly date)
        {
            if (!days.TryGetValue(date, out var day))
                days[date] = day = new RevenueDay();
            return day;
        }

        var showtimeStats = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(s =>
                s.BusinessDate >= fromDate
                && s.BusinessDate <= toDate
                && s.Kind == ShowtimeKind.Regular
            )
            .GroupBy(s => s.BusinessDate)
            .Select(g => new
            {
                Date = g.Key,
                Ticket = g.Sum(s => s.GrossTicketRevenue - s.DiscountAmount - s.RefundAmount),
                Combo = g.Sum(s => s.ComboRevenue),
            })
            .ToListAsync(cancellationToken);
        foreach (var row in showtimeStats)
        {
            var day = DayOf(row.Date);
            day.Ticket += row.Ticket;
            day.Combo += row.Combo;
        }

        var showtimeOrders = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.ShowtimeId != null
                && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                && l.Order.Status == OrderStatus.Confirmed
                && l.Showtime!.StartTime >= fromUtc
                && l.Showtime.StartTime < toUtc
                && !db.ShowtimeDailyStats.Any(d =>
                    d.ShowtimeId == l.ShowtimeId && d.Kind != ShowtimeKind.Regular
                )
            )
            .Select(l => new { l.OrderId, l.Showtime!.StartTime })
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var o in showtimeOrders)
            DayOf(BusinessDay.Of(o.StartTime, settings.OpeningTime, settings.ClosingTime))
                .Orders.Add(o.OrderId);

        var standaloneLines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.ShowtimeId == null
                && (
                    l.LineType == OrderLineType.Combo
                    || (
                        l.LineType == OrderLineType.Fee
                        && l.Order.Type == OrderType.Supplement
                    )
                )
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
            .Select(l => new
            {
                l.OrderId,
                l.OrderLineId,
                l.LineType,
                l.PaidAmount,
                l.RefundedAmount,
                OrderStatus = l.Order.Status,
                PaidAt = l
                    .Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                        && t.CompletedAt != null
                    )
                    .Min(t => t.CompletedAt) ?? l.Order.CreatedAt,
                Charged = l
                    .Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                Refunded = l
                    .Order.PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Refund
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .Where(l => l.PaidAt >= fromUtc && l.PaidAt < toUtc)
            .ToListAsync(cancellationToken);

        foreach (var order in standaloneLines.GroupBy(l => l.OrderId))
        {
            var lines = order.OrderBy(l => l.PaidAmount).ThenBy(l => l.OrderLineId).ToList();
            var cancelled = lines[0].OrderStatus == OrderStatus.Cancelled;
            var refunds = cancelled
                ? RefundShares(lines.Select(l => l.PaidAmount).ToList(), lines[0].Charged, lines[0].Refunded)
                : lines.Select(l => Math.Min(l.PaidAmount, l.RefundedAmount)).ToArray();

            var day = DayOf(
                BusinessDay.Of(lines[0].PaidAt, settings.OpeningTime, settings.ClosingTime)
            );
            for (var i = 0; i < lines.Count; i++)
            {
                var net = lines[i].PaidAmount - refunds[i];
                if (lines[i].LineType == OrderLineType.Combo)
                    day.Combo += net;
                else
                    day.Ticket += net;
            }

            if (!cancelled)
                day.Orders.Add(order.Key);
        }

        var payments = await db
            .PaymentDailyStats.AsNoTracking()
            .Where(p => p.BusinessDate >= fromDate && p.BusinessDate <= toDate)
            .GroupBy(p => p.BusinessDate)
            .Select(g => new
            {
                Date = g.Key,
                Collected = g.Sum(p => p.CollectedAmount),
                Refunded = g.Sum(p => p.RefundedAmount),
            })
            .ToListAsync(cancellationToken);
        foreach (var row in payments)
        {
            var day = DayOf(row.Date);
            day.Collected += row.Collected;
            day.Refunded += row.Refunded;
        }

        var items = days
            .GroupBy(d => DateTimeUtils.GetPeriodKey(d.Key, groupBy))
            .OrderBy(g => g.Min(d => d.Key))
            .Select(g =>
            {
                var ticket = g.Sum(d => d.Value.Ticket);
                var combo = g.Sum(d => d.Value.Combo);
                return new RevenueReportItemDto(
                    g.Key,
                    ticket,
                    combo,
                    ticket + combo,
                    g.SelectMany(d => d.Value.Orders).Distinct().Count(),
                    g.Sum(d => d.Value.Collected),
                    g.Sum(d => d.Value.Refunded)
                );
            })
            .ToList();

        var summary = new RevenueSummaryDto(
            items.Sum(i => i.TotalRevenue),
            days.Values.SelectMany(d => d.Orders).Distinct().Count(),
            items.Sum(i => i.CollectedAmount),
            items.Sum(i => i.RefundedAmount)
        );

        return new RevenueReportDto(groupBy, items, summary);
    }

    private static decimal[] RefundShares(
        IReadOnlyList<decimal> paidAmounts,
        decimal charged,
        decimal refunded
    )
    {
        if (charged <= 0m)
            return new decimal[paidAmounts.Count];

        var total = paidAmounts.Sum();
        var ratio = Math.Min(1m, refunded / charged);
        return OrderPricing.AllocateProportionally(
            OrderPricing.RoundToDong(total * ratio),
            paidAmounts
        );
    }

    private sealed class RevenueDay
    {
        public decimal Ticket { get; set; }
        public decimal Combo { get; set; }
        public decimal Collected { get; set; }
        public decimal Refunded { get; set; }
        public HashSet<int> Orders { get; } = [];
    }

    public async Task<List<TopMovieDto>> GetTopMoviesReportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var fromDateVal = DateTimeUtils.GetStartOfDay(fromDate);
        var toDateVal = DateTimeUtils.GetStartOfNextDay(toDate);

        var query = db
            .OrderLines.ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.Showtime != null
                && l.Order.Status == OrderStatus.Confirmed
                && l.Order.CreatedAt >= fromDateVal
                && l.Order.CreatedAt < toDateVal
            )
            .GroupBy(l => new { l.Showtime!.MovieId, l.Showtime.Movie.Title })
            .Select(g => new
            {
                g.Key.MovieId,
                g.Key.Title,
                TicketsSold = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.PaidAmount - l.RefundedAmount),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(limit);

        var results = await query.ToListAsync(cancellationToken);
        return results.Adapt<List<TopMovieDto>>();
    }

    public async Task<List<EmployeePerformanceDto>> GetEmployeePerformanceReportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        var fromDateVal = DateTimeUtils.GetStartOfDay(fromDate);
        var toDateVal = DateTimeUtils.GetStartOfNextDay(toDate);

        var orders = await db
            .Orders.Where(o =>
                o.Status == OrderStatus.Confirmed
                && o.Channel == SalesChannel.Counter
                && o.CreatedByEmployeeId != null
                && o.CreatedAt >= fromDateVal
                && o.CreatedAt < toDateVal
            )
            .GroupBy(o => new { o.CreatedByEmployeeId, o.CreatedByEmployee!.User.FullName })
            .Select(g => new
            {
                EmployeeId = g.Key.CreatedByEmployeeId!.Value,
                g.Key.FullName,
                Revenue = g.Sum(o => o.FinalAmount),
                BookingCount = g.Count(o => o.Type != OrderType.Exchange),
            })
            .ToListAsync(cancellationToken);

        var ticketsByEmployee = await db
            .OrderLines.ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.Order.Status == OrderStatus.Confirmed
                && l.Order.Channel == SalesChannel.Counter
                && l.Order.CreatedByEmployeeId != null
                && l.Order.CreatedAt >= fromDateVal
                && l.Order.CreatedAt < toDateVal
            )
            .GroupBy(l => l.Order.CreatedByEmployeeId!.Value)
            .Select(g => new { EmployeeId = g.Key, Tickets = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Tickets, cancellationToken);

        return orders
            .OrderByDescending(x => x.Revenue)
            .Select(x => new EmployeePerformanceDto(
                x.EmployeeId,
                x.FullName,
                ticketsByEmployee.GetValueOrDefault(x.EmployeeId),
                x.Revenue,
                x.BookingCount
            ))
            .ToList();
    }

    public async Task<ComboReportDto> GetComboReportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        if (toDate < fromDate)
            throw new ValidationAppException(
                Messages.Validation_To_BeforeFrom,
                new Dictionary<string, string[]> { ["toDate"] = [Messages.Validation_To_BeforeFrom] }
            );

        var fromDateVal = DateTimeUtils.GetStartOfDay(fromDate);
        var toDateVal = DateTimeUtils.GetStartOfNextDay(toDate);

        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Combo
                && l.ComboId != null
                && l.Order.Status == OrderStatus.Confirmed
            )
            .Select(l => new
            {
                ComboId = l.ComboId!.Value,
                ComboName = l.Combo!.Name,
                l.Quantity,
                l.RefundedQuantity,
                l.QuantityFulfilled,
                l.PaidAmount,
                l.RefundedAmount,
                WithTickets = l.ShowtimeId != null,
                RecognizedAt = l.Showtime != null ? l.Showtime.StartTime : l.Order.CreatedAt,
            })
            .Where(l => l.RecognizedAt >= fromDateVal && l.RecognizedAt < toDateVal)
            .ToListAsync(cancellationToken);

        var items = lines
            .GroupBy(l => new { l.ComboId, l.ComboName })
            .Select(g =>
            {
                var sold = g.Sum(l => l.Quantity - l.RefundedQuantity);
                var delivered = g.Sum(l => l.QuantityFulfilled);
                return new ComboReportItemDto(
                    g.Key.ComboId,
                    g.Key.ComboName,
                    sold,
                    delivered,
                    Math.Max(0, sold - delivered),
                    g.Where(l => l.WithTickets).Sum(l => l.Quantity - l.RefundedQuantity),
                    g.Where(l => !l.WithTickets).Sum(l => l.Quantity - l.RefundedQuantity),
                    g.Sum(l => l.PaidAmount - l.RefundedAmount)
                );
            })
            .OrderByDescending(i => i.Revenue)
            .ThenBy(i => i.ComboName)
            .ToList();

        var summary = new ComboReportSummaryDto(
            items.Sum(i => i.QuantitySold),
            items.Sum(i => i.QuantityDelivered),
            items.Sum(i => i.QuantityPending),
            items.Sum(i => i.Revenue)
        );
        return new ComboReportDto(fromDate, toDate, items, summary);
    }
}
