using Cinema.Application.Caching;
using Cinema.Application.DTOs.DailyClosings;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.DailyClosings;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Services.Reports.Finance;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.DailyClosings;

public class DailyClosingReportService(
    IAppDbContext db,
    IAppCache cache,
    IEnumerable<IReportBlock> registeredBlocks
) : IDailyClosingReportService
{
    private static bool IsCovered(IReportBlock block) =>
        block
            is RevenueByChannelBlock
                or DiscountBlock
                or RefundCancellationBlock
                or GatewayFeeBlock
                or ComboProfitBlock
                or MovieProfitBlock;

    public async Task<DailyClosingSnapshot> BuildDraftAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var today = BusinessDay.Of(DateTime.UtcNow, settings.OpeningTime, settings.ClosingTime);
        if (businessDate == default || businessDate > today)
            throw new BusinessRuleException(Messages.Rule_DailyClosingDateInvalid);

        if (
            await db
                .DailyClosings.AsNoTracking()
                .AnyAsync(
                    c => c.BusinessDate == businessDate && c.Status == DailyClosingStatus.Closed,
                    cancellationToken
                )
        )
            throw new ConflictAppException(Messages.Conflict_DailyClosingAlreadyClosed);

        return await BuildSnapshotAsync(businessDate, cancellationToken);
    }

    public async Task<DailyClosingSnapshot> BuildSnapshotAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(businessDate, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(businessDate, settings.OpeningTime, settings.ClosingTime);

        var lines = await RecognizedRevenueLoader.LoadAsync(
            db,
            settings,
            businessDate,
            businessDate,
            cancellationToken
        );
        var buckets = await MethodRevenueAllocator.AllocateAsync(db, lines, cancellationToken);

        var payments = await db
            .PaymentDailyStats.AsNoTracking()
            .Where(p => p.BusinessDate == businessDate)
            .ToListAsync(cancellationToken);
        var counts = new Dictionary<(SalesChannel, PaymentMethod?), int>();
        foreach (var p in payments)
        {
            var key = (p.Channel, (PaymentMethod?)p.Method);
            if (!buckets.TryGetValue(key, out var bucket))
                buckets[key] = bucket = new RevenueBucket();
            bucket.Collected += p.CollectedAmount;
            bucket.Refunded += p.RefundedAmount;
            counts[key] = p.TransactionCount;
        }

        var methods = buckets
            .OrderBy(e => e.Key.Channel)
            .ThenBy(e => e.Key.Method is null)
            .ThenBy(e => e.Key.Method)
            .Select(e => new DailyClosingMethodDto(
                e.Key.Channel.ToString(),
                e.Key.Method?.ToString(),
                e.Value.Ticket,
                e.Value.Combo,
                e.Value.GiftCard,
                e.Value.Collected,
                e.Value.Refunded,
                counts.GetValueOrDefault(e.Key)
            ))
            .ToList();

        var ticket = lines.Where(l => l.Kind == RevenueKind.Ticket).Sum(l => l.Net);
        var combo = lines.Where(l => l.Kind == RevenueKind.Combo).Sum(l => l.Net);
        var revenue = new DailyClosingRevenueDto(
            ticket,
            combo,
            ticket + combo,
            lines.Where(l => l.Kind == RevenueKind.GiftCard).Sum(l => l.Net),
            payments.Where(p => p.Method == PaymentMethod.GiftCard).Sum(p => p.CollectedAmount)
        );

        var tier = lines.Sum(l => l.TierDiscount);
        var promotion = lines.Sum(l => l.PromotionDiscount);
        var other = lines.Sum(l => l.OtherDiscount);
        var discounts = new DailyClosingDiscountDto(tier, promotion, other, tier + promotion + other);

        var showtimeStats = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(s => s.BusinessDate == businessDate && s.Kind == ShowtimeKind.Regular)
            .GroupBy(s => 1)
            .Select(g => new { Refund = g.Sum(s => s.RefundAmount), Cancelled = g.Sum(s => s.CancelledOrders) })
            .FirstOrDefaultAsync(cancellationToken);
        var refunds = new DailyClosingRefundDto(
            payments.Sum(p => p.RefundedAmount),
            showtimeStats?.Refund ?? 0m,
            showtimeStats?.Cancelled ?? 0
        );

        var orders = await LoadOrdersAsync(fromUtc, toUtc, cancellationToken);
        var cash = await LoadCashAsync(fromUtc, toUtc, cancellationToken);
        var blocks = await LoadExtraBlocksAsync(
            new ReportPeriod(businessDate, businessDate),
            cancellationToken
        );

        return new DailyClosingSnapshot(
            businessDate,
            DateTime.UtcNow,
            revenue,
            discounts,
            refunds,
            methods,
            orders,
            cash,
            blocks
        );
    }

    private async Task<DailyClosingOrderDto> LoadOrdersAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken
    )
    {
        var dayOrders = db
            .Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt < toUtc);

        var pending = await dayOrders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
        var problem = await dayOrders.CountAsync(
            o =>
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Expired)
                && (
                    o.PaymentTransactions
                        .Where(t =>
                            t.Type == PaymentTransactionType.Charge && t.Status == PaymentStatus.Success
                        )
                        .Sum(t => (decimal?)t.Amount) ?? 0m
                )
                    > (
                        o.PaymentTransactions
                            .Where(t =>
                                t.Type == PaymentTransactionType.Refund
                                && t.Status == PaymentStatus.Success
                            )
                            .Sum(t => (decimal?)t.Amount) ?? 0m
                    ),
            cancellationToken
        );
        return new DailyClosingOrderDto(pending, problem);
    }

    private async Task<DailyClosingCashDto> LoadCashAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken
    )
    {
        var shifts = await db
            .CounterShifts.AsNoTracking()
            .Where(s => s.OpenedAt >= fromUtc && s.OpenedAt < toUtc)
            .OrderBy(s => s.OpenedAt)
            .ThenBy(s => s.CounterShiftId)
            .Select(s => new
            {
                Shift = s,
                EmployeeName = s.Employee.User.FullName,
            })
            .ToListAsync(cancellationToken);

        var rows = new List<DailyClosingShiftDto>();
        decimal opening = 0m,
            collected = 0m,
            refunded = 0m,
            change = 0m,
            expectedTotal = 0m;
        foreach (var item in shifts)
        {
            var s = item.Shift;
            var totals = await db.LoadCounterShiftCashTotalsAsync(s.CounterShiftId, cancellationToken);
            var expected = s.ExpectedCash ?? totals.Expected(s.OpeningCash);

            opening += s.OpeningCash;
            collected += totals.Collected;
            refunded += totals.Refunded;
            change += totals.ChangeGiven;
            expectedTotal += expected;
            rows.Add(
                new DailyClosingShiftDto(
                    s.CounterShiftId,
                    item.EmployeeName,
                    s.Status.ToString(),
                    s.OpeningCash,
                    expected,
                    s.CountedCash,
                    s.Variance
                )
            );
        }

        return new DailyClosingCashDto(
            rows.Count,
            shifts.Count(s => s.Shift.Status == CounterShiftStatus.Open),
            opening,
            collected,
            refunded,
            change,
            expectedTotal,
            rows
        );
    }

    private async Task<IReadOnlyList<ReportBlockResult>> LoadExtraBlocksAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var results = new List<ReportBlockResult>();
        foreach (
            var block in registeredBlocks
                .Where(b => !IsCovered(b))
                .OrderBy(b => b.Order)
                .ThenBy(b => b.Key, StringComparer.Ordinal)
        )
        {
            if (!await block.HasDataAsync(period, cancellationToken))
                continue;
            results.Add(await block.BuildAsync(period, cancellationToken));
        }

        return results;
    }
}
