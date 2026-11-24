using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class CancellationAnalyticsService(IAppDbContext db) : ICancellationAnalyticsService
{
    private const int MaxDays = 366;
    private const string Unspecified = "Unspecified";
    private const int NoMovie = 0;

    private sealed class OrderInfo
    {
        public int OrderId { get; init; }
        public CancelReason? Reason { get; init; }
        public CancelledBy? By { get; init; }
        public DateTime? CancelledAt { get; init; }
        public DateTime? ShowStart { get; init; }
        public int? MovieId { get; init; }
        public decimal Refunded { get; init; }
    }

    public async Task<CancellationAnalyticsDto> GetAsync(
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

        // Đơn đã từng thu tiền (xác nhận hoặc đã hủy). Đơn vé/combo, ghi nhận theo ngày chiếu
        // (đơn không có suất thì theo ngày tạo), cùng quy tắc với báo cáo combo.
        var paidOrders = db
            .Orders.AsNoTracking()
            .Where(o =>
                (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Cancelled)
                && (o.Type == OrderType.Ticket || o.Type == OrderType.Combo)
                && o.PaymentTransactions.Any(t =>
                    t.Type == PaymentTransactionType.Charge && t.Status == PaymentStatus.Success
                )
            );

        var paid = Project(paidOrders, fromUtc, toUtc);
        var cancelled = Project(
            CancellationRules.ExcludingExchanges(
                paidOrders.Where(o => o.Status == OrderStatus.Cancelled)
            ),
            fromUtc,
            toUtc
        );

        var paidCount = await paid.CountAsync(cancellationToken);
        var paidByMovie = (
            await paid
                .GroupBy(x => x.MovieId)
                .Select(g => new { MovieId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken)
        ).ToDictionary(x => x.MovieId ?? NoMovie, x => x.Count);

        var rows = await cancelled.ToListAsync(cancellationToken);

        var movieIds = paidByMovie.Keys
            .Concat(rows.Select(r => r.MovieId ?? NoMovie))
            .Where(id => id != NoMovie)
            .Distinct()
            .ToList();
        var titles = movieIds.Count == 0
            ? new Dictionary<int, string>()
            : await db
                .Movies.AsNoTracking()
                .Where(m => movieIds.Contains(m.MovieId))
                .ToDictionaryAsync(m => m.MovieId, m => m.Title, cancellationToken);

        var totalRefunded = rows.Sum(r => r.Refunded);

        var byReason = Groups(
            rows,
            r => r.Reason?.ToString() ?? Unspecified,
            Enum.GetNames<CancelReason>(),
            totalRefunded
        );
        var byActor = Groups(
            rows,
            r => r.By?.ToString() ?? Unspecified,
            Enum.GetNames<CancelledBy>(),
            totalRefunded
        );
        var byTime = Groups(rows, TimeBucket, CancellationTimeBuckets.All, totalRefunded);

        var byMovie = rows
            .Select(r => r.MovieId ?? NoMovie)
            .Union(paidByMovie.Keys)
            .Select(movieId =>
            {
                var group = rows.Where(r => (r.MovieId ?? NoMovie) == movieId).ToList();
                var paidOfMovie = paidByMovie.GetValueOrDefault(movieId);
                return new CancellationMovieDto(
                    movieId == NoMovie ? null : movieId,
                    titles.GetValueOrDefault(movieId),
                    paidOfMovie,
                    group.Count,
                    Percent(group.Count, paidOfMovie),
                    group.Sum(r => r.Refunded)
                );
            })
            .OrderByDescending(x => x.CancelledOrders)
            .ThenBy(x => x.MovieTitle)
            .ToList();

        return new CancellationAnalyticsDto(
            from,
            to,
            new CancellationTotalDto(
                paidCount,
                rows.Count,
                Percent(rows.Count, paidCount),
                totalRefunded
            ),
            byReason,
            byActor,
            byTime,
            byMovie
        );
    }

    private static IQueryable<OrderInfo> Project(
        IQueryable<Order> orders,
        DateTime fromUtc,
        DateTime toUtc
    ) =>
        orders
            .Select(o => new
            {
                o.OrderId,
                o.CreatedAt,
                o.CancelReason,
                o.CancelledBy,
                o.CancelledAt,
                ShowStart = o
                    .Lines.Where(l =>
                        l.ShowtimeId != null
                        && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                    )
                    .Min(l => (DateTime?)l.Showtime!.StartTime),
                MovieId = o
                    .Lines.Where(l =>
                        l.ShowtimeId != null
                        && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                    )
                    .OrderBy(l => l.Showtime!.StartTime)
                    .Select(l => (int?)l.Showtime!.MovieId)
                    .FirstOrDefault(),
                Refunded = o
                    .PaymentTransactions.Where(t =>
                        t.Type == PaymentTransactionType.Refund
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .Where(x => (x.ShowStart ?? x.CreatedAt) >= fromUtc && (x.ShowStart ?? x.CreatedAt) < toUtc)
            .Select(x => new OrderInfo
            {
                OrderId = x.OrderId,
                Reason = x.CancelReason,
                By = x.CancelledBy,
                CancelledAt = x.CancelledAt,
                ShowStart = x.ShowStart,
                MovieId = x.MovieId,
                Refunded = x.Refunded,
            });

    private static string TimeBucket(OrderInfo r)
    {
        if (r.ShowStart is not { } start || r.CancelledAt is not { } at)
            return CancellationTimeBuckets.NoShowtime;
        var hours = (start - at).TotalHours;
        return hours switch
        {
            < 3 => CancellationTimeBuckets.Under3Hours,
            < 24 => CancellationTimeBuckets.Hours3To24,
            < 72 => CancellationTimeBuckets.Days1To3,
            _ => CancellationTimeBuckets.Over3Days,
        };
    }

    private static List<CancellationGroupDto> Groups(
        List<OrderInfo> rows,
        Func<OrderInfo, string> keySelector,
        IEnumerable<string> allKeys,
        decimal totalRefunded
    )
    {
        var grouped = rows.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToList());
        var keys = allKeys.Concat(grouped.Keys).Distinct().ToList();
        return keys
            .Select(key =>
            {
                var list = grouped.GetValueOrDefault(key) ?? [];
                var refunded = list.Sum(r => r.Refunded);
                return new CancellationGroupDto(
                    key,
                    list.Count,
                    refunded,
                    Percent(list.Count, rows.Count)
                );
            })
            .ToList();
    }

    private static decimal? Percent(int part, int whole) =>
        whole > 0
            ? Math.Round((decimal)part / whole * 100m, 2, MidpointRounding.AwayFromZero)
            : null;
}
