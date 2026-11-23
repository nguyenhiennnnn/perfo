using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports;

public class ReportService(IAppDbContext db) : IReportService
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

        var fromDateVal = DateTimeUtils.GetStartOfDay(fromDate);
        var toDateVal = DateTimeUtils.GetStartOfNextDay(toDate);

        var charges = await db
            .PaymentTransactions.SuccessfulChargesCompletedBetween(fromDateVal, toDateVal)
            .Select(t => new
            {
                t.OrderId,
                CompletedAt = t.CompletedAt!.Value,
                t.Amount,
                Refunded = t
                    .Refunds.Where(r =>
                        r.Type == PaymentTransactionType.Refund
                        && r.Status == PaymentStatus.Success
                    )
                    .Sum(r => (decimal?)r.Amount) ?? 0m,
                OrderFinalAmount = t.Order.FinalAmount,
                TicketRevenue = t
                    .Order.Lines.Where(l =>
                        l.LineType == OrderLineType.Ticket
                        || (
                            l.LineType == OrderLineType.Fee
                            && l.Order.Type == OrderType.Supplement
                        )
                    )
                    .Sum(l => (decimal?)l.PaidAmount) ?? 0m,
                ComboRevenue = t
                    .Order.Lines.Where(l => l.LineType == OrderLineType.Combo)
                    .Sum(l => (decimal?)l.PaidAmount) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        var rows = charges
            .Select(c =>
            {
                var net = ChargeRevenueQuery.NetOf(c.Amount, c.Refunded);
                var ratio =
                    c.OrderFinalAmount > 0m ? Math.Min(1m, net / c.OrderFinalAmount) : 0m;
                return new
                {
                    c.OrderId,
                    PaidAt = c.CompletedAt,
                    Net = net,
                    TicketRevenue = c.TicketRevenue * ratio,
                    ComboRevenue = c.ComboRevenue * ratio,
                };
            })
            .Where(r => r.Net > 0m)
            .ToList();

        var items = rows.GroupBy(r => DateTimeUtils.GetPeriodKey(r.PaidAt, groupBy))
            .OrderBy(g => g.Min(x => x.PaidAt))
            .Select(g => new
            {
                Period = g.Key,
                TicketRevenue = g.Sum(x => x.TicketRevenue),
                ComboRevenue = g.Sum(x => x.ComboRevenue),
                TotalRevenue = g.Sum(x => x.Net),
                BookingCount = g.Select(x => x.OrderId).Distinct().Count(),
            })
            .Adapt<List<RevenueReportItemDto>>();

        var summary = new
        {
            TotalRevenue = rows.Sum(r => r.Net),
            TotalBookings = rows.Select(r => r.OrderId).Distinct().Count(),
        }.Adapt<RevenueSummaryDto>();

        return new RevenueReportDto(groupBy, items, summary);
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
            .OrderLines.Where(l =>
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
                BookingCount = g.Count(),
            })
            .ToListAsync(cancellationToken);

        var ticketsByEmployee = await db
            .OrderLines.Where(l =>
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
}
