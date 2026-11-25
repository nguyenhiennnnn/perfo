using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces.Combos;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class ComboAnalyticsService(IAppDbContext db) : IComboAnalyticsService
{
    private const int MaxDays = 366;

    private sealed record Row(
        int ComboId,
        SalesChannel Channel,
        int Sold,
        int Fulfilled,
        decimal Revenue
    );

    private readonly record struct Sums(int Sold, int Fulfilled, decimal Revenue)
    {
        public static Sums operator +(Sums a, Sums b) =>
            new(a.Sold + b.Sold, a.Fulfilled + b.Fulfilled, a.Revenue + b.Revenue);
    }

    public async Task<ComboRevenueDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    )
    {
        if (from == default || to == default || from > to)
            throw new BusinessRuleException(Messages.Rule_OccupancyRangeInvalid);
        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_OccupancyRangeTooLong, MaxDays)
            );

        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        var fromUtc = BusinessDay.StartUtc(from, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(to, settings.OpeningTime, settings.ClosingTime);

        // Combo kèm vé: ghi nhận theo ngày chiếu; combo bán riêng: theo ngày bán.
        var comboLines = db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Combo
                && l.ComboId != null
                && l.Order.Status == OrderStatus.Confirmed
                && (l.ShowtimeId != null ? l.Showtime!.StartTime : l.Order.CreatedAt) >= fromUtc
                && (l.ShowtimeId != null ? l.Showtime!.StartTime : l.Order.CreatedAt) < toUtc
            );

        var rows = await comboLines
            .GroupBy(l => new { ComboId = l.ComboId!.Value, l.Order.Channel })
            .Select(g => new Row(
                g.Key.ComboId,
                g.Key.Channel,
                g.Sum(l => l.Quantity - l.RefundedQuantity),
                g.Sum(l =>
                    l.QuantityFulfilled < l.Quantity - l.RefundedQuantity
                        ? l.QuantityFulfilled
                        : l.Quantity - l.RefundedQuantity
                ),
                g.Sum(l => l.PaidAmount - l.RefundedAmount)
            ))
            .ToListAsync(cancellationToken);

        var ordersWithCombo = await comboLines
            .Where(l => l.Quantity - l.RefundedQuantity > 0)
            .Select(l => l.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Đơn xác nhận trong kỳ: đơn vé hoặc đơn combo có dòng vé/combo rơi vào kỳ (cùng quy tắc ngày).
        var confirmedOrders = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                && (l.Order.Type == OrderType.Ticket || l.Order.Type == OrderType.Combo)
                && l.Order.Status == OrderStatus.Confirmed
                && (l.ShowtimeId != null ? l.Showtime!.StartTime : l.Order.CreatedAt) >= fromUtc
                && (l.ShowtimeId != null ? l.Showtime!.StartTime : l.Order.CreatedAt) < toUtc
            )
            .Select(l => l.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);

        var comboIds = rows.Select(r => r.ComboId).Distinct().ToList();
        var names = comboIds.Count == 0
            ? new Dictionary<int, string>()
            : await db
                .Combos.AsNoTracking()
                .Where(c => comboIds.Contains(c.ComboId))
                .ToDictionaryAsync(c => c.ComboId, c => c.Name, cancellationToken);

        var byCombo = rows
            .GroupBy(r => r.ComboId)
            .Select(g =>
            {
                var s = Sum(g);
                return new ComboRevenueItemDto(
                    g.Key,
                    names.GetValueOrDefault(g.Key),
                    s.Sold,
                    s.Fulfilled,
                    Percent(s.Fulfilled, s.Sold),
                    s.Revenue
                );
            })
            .OrderByDescending(x => x.NetRevenue)
            .ThenBy(x => x.ComboName)
            .ToList();

        var byChannel = new[] { SalesChannel.Online, SalesChannel.Counter }
            .Select(ch =>
            {
                var s = Sum(rows.Where(r => r.Channel == ch));
                return new ComboRevenueChannelDto(
                    ch,
                    s.Sold,
                    s.Fulfilled,
                    Percent(s.Fulfilled, s.Sold),
                    s.Revenue
                );
            })
            .ToList();

        var total = Sum(rows);
        return new ComboRevenueDto(
            from,
            to,
            new ComboRevenueTotalDto(
                total.Sold,
                total.Fulfilled,
                Percent(total.Fulfilled, total.Sold),
                total.Revenue,
                confirmedOrders,
                ordersWithCombo,
                confirmedOrders > 0 ? Percent(ordersWithCombo, confirmedOrders) : null,
                ordersWithCombo > 0
                    ? Math.Round(
                        total.Revenue / ordersWithCombo,
                        0,
                        MidpointRounding.AwayFromZero
                    )
                    : null
            ),
            byCombo,
            byChannel
        );
    }

    private static Sums Sum(IEnumerable<Row> rows) =>
        rows.Aggregate(default(Sums), (a, r) => a + new Sums(r.Sold, r.Fulfilled, r.Revenue));

    private static decimal? Percent(int part, int whole) =>
        whole > 0
            ? Math.Round((decimal)part / whole * 100m, 2, MidpointRounding.AwayFromZero)
            : null;
}
