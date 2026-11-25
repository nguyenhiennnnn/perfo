using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class GiftCardLiabilityBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "gift-card-liability";
    public string Title => Messages.Report_Finance_GiftCardLiability_Title;
    public int Order => 70;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (_, toUtc) = await RangeAsync(period, cancellationToken);
        return await db
            .GiftCardLedgers.AsNoTracking()
            .AnyAsync(
                l => l.EntryType == GiftCardLedgerType.Issue && l.OccurredAt < toUtc,
                cancellationToken
            );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await RangeAsync(period, cancellationToken);

        var opening = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l => l.OccurredAt < fromUtc && l.BalanceChange != 0m)
            .GroupBy(l => l.GiftCard.Type)
            .Select(g => new { Type = g.Key, Amount = g.Sum(l => l.BalanceChange) })
            .ToListAsync(cancellationToken);

        var movements = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l => l.OccurredAt >= fromUtc && l.OccurredAt < toUtc && l.BalanceChange != 0m)
            .GroupBy(l => new { l.GiftCard.Type, l.EntryType })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.EntryType,
                Amount = g.Sum(l => l.BalanceChange),
            })
            .ToListAsync(cancellationToken);

        var outstandingCards = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l => l.OccurredAt < toUtc && l.BalanceChange != 0m)
            .GroupBy(l => new { l.GiftCardId, l.GiftCard.Type })
            .Where(g => g.Sum(l => l.BalanceChange) > 0m)
            .Select(g => g.Key.Type)
            .ToListAsync(cancellationToken);

        decimal Move(GiftCardType type, GiftCardLedgerType entry) =>
            movements.Where(m => m.Type == type && m.EntryType == entry).Sum(m => m.Amount);

        var lines = Enum.GetValues<GiftCardType>()
            .Select(type =>
            {
                var open = opening.Where(o => o.Type == type).Sum(o => o.Amount);
                var issued = Move(type, GiftCardLedgerType.Issue);
                var redeemed = Move(type, GiftCardLedgerType.Redeem);
                var refunded = Move(type, GiftCardLedgerType.Refund);
                var cancelled = Move(type, GiftCardLedgerType.Cancel);
                var expired = Move(type, GiftCardLedgerType.Expire);
                return new Line(
                    type,
                    outstandingCards.Count(t => t == type),
                    open,
                    issued,
                    redeemed,
                    refunded,
                    cancelled,
                    expired,
                    open + issued + redeemed + refunded + cancelled + expired
                );
            })
            .Where(l =>
                l.Cards > 0
                || l.Opening != 0m
                || l.Issued != 0m
                || l.Redeemed != 0m
                || l.Refunded != 0m
                || l.Cancelled != 0m
                || l.Expired != 0m
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("type", Messages.Report_Finance_Col_GiftCardType, ReportColumnType.Text),
            new("cards", Messages.Report_Finance_Col_GiftCardCards, ReportColumnType.Number),
            new("opening", Messages.Report_Finance_Col_GiftCardOpening, ReportColumnType.Money),
            new("issued", Messages.Report_Finance_Col_GiftCardIssued, ReportColumnType.Money),
            new("redeemed", Messages.Report_Finance_Col_GiftCardRedeemed, ReportColumnType.Money),
            new("refunded", Messages.Report_Finance_Col_GiftCardRefunded, ReportColumnType.Money),
            new("cancelled", Messages.Report_Finance_Col_GiftCardCancelled, ReportColumnType.Money),
            new("expired", Messages.Report_Finance_Col_GiftCardExpired, ReportColumnType.Money),
            new("closing", Messages.Report_Finance_Col_GiftCardClosing, ReportColumnType.Money),
        ];

        var rows = lines
            .Select(l =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["type"] = TypeLabel(l.Type),
                        ["cards"] = l.Cards,
                        ["opening"] = l.Opening,
                        ["issued"] = l.Issued,
                        ["redeemed"] = l.Redeemed,
                        ["refunded"] = l.Refunded,
                        ["cancelled"] = l.Cancelled,
                        ["expired"] = l.Expired,
                        ["closing"] = l.Closing,
                    }
            )
            .ToList();

        var totals = new Dictionary<string, object?>
        {
            ["type"] = Messages.Export_Total,
            ["cards"] = lines.Sum(l => l.Cards),
            ["opening"] = lines.Sum(l => l.Opening),
            ["issued"] = lines.Sum(l => l.Issued),
            ["redeemed"] = lines.Sum(l => l.Redeemed),
            ["refunded"] = lines.Sum(l => l.Refunded),
            ["cancelled"] = lines.Sum(l => l.Cancelled),
            ["expired"] = lines.Sum(l => l.Expired),
            ["closing"] = lines.Sum(l => l.Closing),
        };

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            totals,
            [Messages.Report_Finance_GiftCardLiability_Note]
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

    private static string TypeLabel(GiftCardType type) =>
        type == GiftCardType.ExchangeCredit
            ? Messages.Report_Finance_GiftCardLiability_TypeExchangeCredit
            : Messages.Report_Finance_GiftCardLiability_TypeGift;

    private sealed record Line(
        GiftCardType Type,
        int Cards,
        decimal Opening,
        decimal Issued,
        decimal Redeemed,
        decimal Refunded,
        decimal Cancelled,
        decimal Expired,
        decimal Closing
    );
}
