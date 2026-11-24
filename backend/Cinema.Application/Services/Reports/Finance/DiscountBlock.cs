using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class DiscountBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "discounts";
    public string Title => Messages.Report_Finance_Discount_Title;
    public int Order => 20;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        if (
            await db.ShowtimeDailyStats.AnyAsync(
                s =>
                    s.BusinessDate >= period.From
                    && s.BusinessDate <= period.To
                    && s.DiscountAmount > 0m,
                cancellationToken
            )
        )
            return true;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime);
        return await db.OrderPriceAdjustments.AnyAsync(
            a =>
                a.Order.CreatedAt >= fromUtc
                && a.Order.CreatedAt < toUtc
                && a.Allocations.Any(x => x.OrderLine.ShowtimeId == null),
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

        var days = lines
            .Where(l =>
                l.TierDiscount != 0m || l.PromotionDiscount != 0m || l.OtherDiscount != 0m
            )
            .GroupBy(l => l.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Tier = g.Sum(l => l.TierDiscount),
                Promotion = g.Sum(l => l.PromotionDiscount),
                Other = g.Sum(l => l.OtherDiscount),
            })
            .ToList();

        var rows = days.Select(d =>
                (IReadOnlyDictionary<string, object?>)
                    ToRow(
                        d.Date.ToString(DateTimePatterns.Date),
                        d.Tier,
                        d.Promotion,
                        d.Other
                    )
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("date", Messages.Report_Finance_Col_Date, ReportColumnType.Text),
            new("tierDiscount", Messages.Report_Finance_Col_TierDiscount, ReportColumnType.Money),
            new(
                "promotionDiscount",
                Messages.Report_Finance_Col_PromotionDiscount,
                ReportColumnType.Money
            ),
            new("otherDiscount", Messages.Report_Finance_Col_OtherDiscount, ReportColumnType.Money),
            new("totalDiscount", Messages.Export_ColDiscount, ReportColumnType.Money),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(
                Messages.Export_Total,
                days.Sum(d => d.Tier),
                days.Sum(d => d.Promotion),
                days.Sum(d => d.Other)
            )
        );
    }

    private static Dictionary<string, object?> ToRow(
        string date,
        decimal tier,
        decimal promotion,
        decimal other
    ) =>
        new()
        {
            ["date"] = date,
            ["tierDiscount"] = tier,
            ["promotionDiscount"] = promotion,
            ["otherDiscount"] = other,
            ["totalDiscount"] = tier + promotion + other,
        };
}
