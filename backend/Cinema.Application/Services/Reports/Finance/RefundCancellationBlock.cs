using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class RefundCancellationBlock(IAppDbContext db) : IReportBlock
{
    public string Key => "refunds-cancellations";
    public string Title => Messages.Report_Finance_RefundsCancellations_Title;
    public int Order => 30;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        await db.ShowtimeDailyStats.AnyAsync(
            s =>
                s.BusinessDate >= period.From
                && s.BusinessDate <= period.To
                && s.Kind == ShowtimeKind.Regular
                && (s.RefundAmount > 0m || s.CancelledOrders > 0),
            cancellationToken
        )
        || await db.PaymentDailyStats.AnyAsync(
            p => p.BusinessDate >= period.From && p.BusinessDate <= period.To && p.RefundedAmount > 0m,
            cancellationToken
        );

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var showtimes = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(s =>
                s.BusinessDate >= period.From
                && s.BusinessDate <= period.To
                && s.Kind == ShowtimeKind.Regular
            )
            .GroupBy(s => s.BusinessDate)
            .Select(g => new
            {
                Date = g.Key,
                Cancelled = g.Sum(s => s.CancelledOrders),
                Refund = g.Sum(s => s.RefundAmount),
                Revenue = g.Sum(s => s.GrossTicketRevenue - s.DiscountAmount),
            })
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        var payments = await db
            .PaymentDailyStats.AsNoTracking()
            .Where(p => p.BusinessDate >= period.From && p.BusinessDate <= period.To)
            .GroupBy(p => p.BusinessDate)
            .Select(g => new
            {
                Date = g.Key,
                Collected = g.Sum(p => p.CollectedAmount),
                Refunded = g.Sum(p => p.RefundedAmount),
            })
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        int cancelledTotal = 0;
        decimal refundTotal = 0m,
            revenueTotal = 0m,
            collectedTotal = 0m,
            refundedTotal = 0m;

        foreach (var date in showtimes.Keys.Union(payments.Keys).Order())
        {
            var s = showtimes.GetValueOrDefault(date);
            var p = payments.GetValueOrDefault(date);
            var cancelled = s?.Cancelled ?? 0;
            var refund = s?.Refund ?? 0m;
            var revenue = s?.Revenue ?? 0m;
            var collected = p?.Collected ?? 0m;
            var refunded = p?.Refunded ?? 0m;

            cancelledTotal += cancelled;
            refundTotal += refund;
            revenueTotal += revenue;
            collectedTotal += collected;
            refundedTotal += refunded;

            rows.Add(
                ToRow(
                    date.ToString(DateTimePatterns.Date),
                    cancelled,
                    refund,
                    revenue,
                    refunded,
                    collected
                )
            );
        }

        IReadOnlyList<ReportColumn> columns =
        [
            new("date", Messages.Report_Finance_Col_Date, ReportColumnType.Text),
            new("cancelledOrders", Messages.Report_Finance_Col_CancelledOrders, ReportColumnType.Number),
            new("refundAmount", Messages.Report_Finance_Col_RefundAmount, ReportColumnType.Money),
            new(
                "ticketRevenue",
                Messages.Report_Finance_Col_TicketRevenueBeforeRefund,
                ReportColumnType.Money
            ),
            new("refundRate", Messages.Report_Finance_Col_RefundRate, ReportColumnType.Percent),
            new("paymentRefunded", Messages.Report_Finance_Col_PaymentRefunded, ReportColumnType.Money),
            new("paymentCollected", Messages.Report_Finance_Col_PaymentCollected, ReportColumnType.Money),
            new(
                "paymentRefundRate",
                Messages.Report_Finance_Col_PaymentRefundRate,
                ReportColumnType.Percent
            ),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(
                Messages.Export_Total,
                cancelledTotal,
                refundTotal,
                revenueTotal,
                refundedTotal,
                collectedTotal
            )
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string date,
        int cancelled,
        decimal refund,
        decimal revenue,
        decimal refunded,
        decimal collected
    ) =>
        new Dictionary<string, object?>
        {
            ["date"] = date,
            ["cancelledOrders"] = cancelled,
            ["refundAmount"] = refund,
            ["ticketRevenue"] = revenue,
            ["refundRate"] = ReportMath.Percent(refund, revenue),
            ["paymentRefunded"] = refunded,
            ["paymentCollected"] = collected,
            ["paymentRefundRate"] = ReportMath.Percent(refunded, collected),
        };
}
