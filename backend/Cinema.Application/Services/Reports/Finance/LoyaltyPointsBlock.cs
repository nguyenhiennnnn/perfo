using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class LoyaltyPointsBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "loyalty-points";
    public string Title => Messages.Report_Finance_LoyaltyPoints_Title;
    public int Order => 90;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (_, toUtc) = await RangeAsync(period, cancellationToken);
        return await db
            .LoyaltyPointTransactions.AsNoTracking()
            .AnyAsync(t => t.CreatedAt < toUtc, cancellationToken);
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await RangeAsync(period, cancellationToken);

        var opening =
            await db
                .LoyaltyPointTransactions.AsNoTracking()
                .Where(t => t.CreatedAt < fromUtc)
                .SumAsync(t => (long?)t.Points, cancellationToken) ?? 0L;

        var movements = await db
            .LoyaltyPointTransactions.AsNoTracking()
            .Where(t => t.CreatedAt >= fromUtc && t.CreatedAt < toUtc)
            .GroupBy(t => new { t.Type, Positive = t.Points > 0 })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.Positive,
                Count = g.Count(),
                Points = g.Sum(t => t.Points),
            })
            .ToListAsync(cancellationToken);

        (int Count, long Points) Move(LoyaltyTransactionType type, bool? positive = null)
        {
            var matched = movements.Where(m =>
                m.Type == type && (positive is null || m.Positive == positive)
            );
            return (matched.Sum(m => m.Count), matched.Sum(m => (long)m.Points));
        }

        var lines = new List<(string Metric, int? Count, long Points)>
        {
            (Messages.Report_Finance_LoyaltyPoints_Opening, null, opening),
        };
        var earn = Move(LoyaltyTransactionType.Earn);
        var bonus = Move(LoyaltyTransactionType.Bonus);
        var adjustUp = Move(LoyaltyTransactionType.Adjustment, true);
        var redeem = Move(LoyaltyTransactionType.Redeem);
        var expire = Move(LoyaltyTransactionType.Expire);
        var revoke = Move(LoyaltyTransactionType.Revoke);
        var adjustDown = Move(LoyaltyTransactionType.Adjustment, false);

        lines.Add((Messages.Report_Finance_LoyaltyPoints_Earned, earn.Count, earn.Points));
        lines.Add((Messages.Report_Finance_LoyaltyPoints_Bonus, bonus.Count, bonus.Points));
        lines.Add(
            (Messages.Report_Finance_LoyaltyPoints_AdjustmentUp, adjustUp.Count, adjustUp.Points)
        );
        lines.Add((Messages.Report_Finance_LoyaltyPoints_Redeemed, redeem.Count, redeem.Points));
        lines.Add((Messages.Report_Finance_LoyaltyPoints_Expired, expire.Count, expire.Points));
        lines.Add((Messages.Report_Finance_LoyaltyPoints_Revoked, revoke.Count, revoke.Points));
        lines.Add(
            (
                Messages.Report_Finance_LoyaltyPoints_AdjustmentDown,
                adjustDown.Count,
                adjustDown.Points
            )
        );
        lines.Add(
            (
                Messages.Report_Finance_LoyaltyPoints_Closing,
                null,
                opening + lines.Skip(1).Sum(l => l.Points)
            )
        );

        IReadOnlyList<ReportColumn> columns =
        [
            new("metric", Messages.Report_Finance_Col_LoyaltyMetric, ReportColumnType.Text),
            new(
                "transactions",
                Messages.Report_Finance_Col_LoyaltyTransactions,
                ReportColumnType.Number
            ),
            new("points", Messages.Report_Finance_Col_LoyaltyPoints, ReportColumnType.Number),
        ];

        var rows = lines
            .Select(l =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["metric"] = l.Metric,
                        ["transactions"] = l.Count,
                        ["points"] = l.Points,
                    }
            )
            .ToList();

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            null,
            [Messages.Report_Finance_LoyaltyPoints_Note]
        );
    }

    private async Task<(DateTime FromUtc, DateTime ToUtc)> RangeAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return (
            BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime),
            BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime)
        );
    }
}
