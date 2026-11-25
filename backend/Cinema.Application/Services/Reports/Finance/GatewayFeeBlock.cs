using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class GatewayFeeBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "gateway-fees";
    public string Title => Messages.Report_Finance_GatewayFees_Title;
    public int Order => 40;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        db.PaymentDailyStats.AnyAsync(
            p => p.BusinessDate >= period.From && p.BusinessDate <= period.To,
            cancellationToken
        );

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var lines = await RecognizedRevenueLoader.LoadAsync(
            db,
            settings,
            period.From,
            period.To,
            cancellationToken
        );
        var buckets = await MethodRevenueAllocator.AllocateAsync(db, lines, cancellationToken);
        var revenueByMethod = buckets
            .GroupBy(b => b.Key.Method?.ToString() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Value.Ticket + b.Value.Combo));

        var payments = await db
            .PaymentDailyStats.AsNoTracking()
            .Where(p => p.BusinessDate >= period.From && p.BusinessDate <= period.To)
            .GroupBy(p => p.Method)
            .Select(g => new
            {
                Method = g.Key,
                Count = g.Sum(p => p.TransactionCount),
                Collected = g.Sum(p => p.CollectedAmount),
                Fee = g.Sum(p => p.GatewayFee),
            })
            .ToListAsync(cancellationToken);
        var paymentByMethod = payments.ToDictionary(p => p.Method.ToString());

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        int countTotal = 0;
        decimal collectedTotal = 0m,
            feeTotal = 0m,
            revenueTotal = 0m;

        var methods = paymentByMethod
            .Keys.Union(revenueByMethod.Keys)
            .Select(k => k.Length == 0 ? (PaymentMethod?)null : Enum.Parse<PaymentMethod>(k))
            .OrderBy(m => m is null)
            .ThenBy(m => m)
            .ToList();
        foreach (var method in methods)
        {
            var key = method?.ToString() ?? string.Empty;
            var p = paymentByMethod.GetValueOrDefault(key);
            var count = p?.Count ?? 0;
            var collected = p?.Collected ?? 0m;
            var fee = p?.Fee ?? 0m;
            var revenue = revenueByMethod.GetValueOrDefault(key);

            countTotal += count;
            collectedTotal += collected;
            feeTotal += fee;
            revenueTotal += revenue;

            rows.Add(
                ToRow(
                    method is { } m ? ExportLabels.Method(m) : Messages.Report_Finance_Method_Unknown,
                    count,
                    collected,
                    fee,
                    revenue
                )
            );
        }

        IReadOnlyList<ReportColumn> columns =
        [
            new("method", Messages.Export_ColPaymentMethod, ReportColumnType.Text),
            new("transactionCount", Messages.Report_Finance_Col_TransactionCount, ReportColumnType.Number),
            new("collected", Messages.Report_Finance_Col_Collected, ReportColumnType.Money),
            new("gatewayFee", Messages.Report_Finance_Col_GatewayFee, ReportColumnType.Money),
            new("feeRate", Messages.Report_Finance_Col_FeeRate, ReportColumnType.Percent),
            new("revenue", Messages.Report_Finance_Col_RevenueBeforeFee, ReportColumnType.Money),
            new("revenueAfterFee", Messages.Report_Finance_Col_RevenueAfterFee, ReportColumnType.Money),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(Messages.Export_Total, countTotal, collectedTotal, feeTotal, revenueTotal)
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string method,
        int count,
        decimal collected,
        decimal fee,
        decimal revenue
    ) =>
        new Dictionary<string, object?>
        {
            ["method"] = method,
            ["transactionCount"] = count,
            ["collected"] = collected,
            ["gatewayFee"] = fee,
            ["feeRate"] = ReportMath.Percent(fee, collected),
            ["revenue"] = revenue,
            ["revenueAfterFee"] = revenue - fee,
        };
}
