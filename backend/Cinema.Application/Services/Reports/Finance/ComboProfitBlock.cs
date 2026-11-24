using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class ComboProfitBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "combo-profit";
    public string Title => Messages.Report_Finance_ComboProfit_Title;
    public int Order => 50;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        if (
            await db.ShowtimeDailyStats.AnyAsync(
                s =>
                    s.BusinessDate >= period.From
                    && s.BusinessDate <= period.To
                    && s.Kind == ShowtimeKind.Regular
                    && s.ComboRevenue > 0m,
                cancellationToken
            )
        )
            return true;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime);
        return await db.OrderLines.AnyAsync(
            l =>
                l.LineType == OrderLineType.Combo
                && l.ShowtimeId == null
                && l.Order.CreatedAt >= fromUtc
                && l.Order.CreatedAt < toUtc,
            cancellationToken
        );
    }

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

        var combos = lines
            .Where(l => l.Kind == RevenueKind.Combo && l.ComboId != null)
            .GroupBy(l => l.ComboId!.Value)
            .Select(g => new
            {
                Name = g.Select(l => l.ComboName).FirstOrDefault(n => n != null) ?? string.Empty,
                Quantity = g.Sum(l => l.SoldQuantity),
                Revenue = g.Sum(l => l.Net),
                Cost = g.Sum(l => (l.UnitCost ?? 0m) * l.SoldQuantity),
                Missing = g.Count(l => l.UnitCost == null && l.SoldQuantity > 0),
            })
            .OrderByDescending(c => c.Revenue)
            .ThenBy(c => c.Name, StringComparer.CurrentCulture)
            .ToList();

        var rows = combos
            .Select(c =>
                ToRow(c.Name, c.Quantity, c.Revenue, c.Cost, c.Missing)
            )
            .ToList();

        var missingTotal = combos.Sum(c => c.Missing);
        IReadOnlyList<string>? warnings =
            missingTotal > 0
                ? [string.Format(Messages.Report_Finance_Warning_ComboMissingCost, missingTotal)]
                : null;

        IReadOnlyList<ReportColumn> columns =
        [
            new("combo", Messages.Report_Finance_Col_Combo, ReportColumnType.Text),
            new("quantitySold", Messages.Report_Finance_Col_QuantitySold, ReportColumnType.Number),
            new("revenue", Messages.Export_ColComboRevenue, ReportColumnType.Money),
            new("cost", Messages.Report_Finance_Col_Cost, ReportColumnType.Money),
            new("profit", Messages.Report_Finance_Col_Profit, ReportColumnType.Money),
            new("margin", Messages.Report_Finance_Col_Margin, ReportColumnType.Percent),
            new("missingCostLines", Messages.Report_Finance_Col_MissingCostLines, ReportColumnType.Number),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(
                Messages.Export_Total,
                combos.Sum(c => c.Quantity),
                combos.Sum(c => c.Revenue),
                combos.Sum(c => c.Cost),
                missingTotal
            ),
            warnings
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string name,
        int quantity,
        decimal revenue,
        decimal cost,
        int missing
    ) =>
        new Dictionary<string, object?>
        {
            ["combo"] = name,
            ["quantitySold"] = quantity,
            ["revenue"] = revenue,
            ["cost"] = cost,
            ["profit"] = revenue - cost,
            ["margin"] = ReportMath.Percent(revenue - cost, revenue),
            ["missingCostLines"] = missing,
        };
}
