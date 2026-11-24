using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Tax;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class VatReportBlock(IAppDbContext db, IAppCache cache, IVatService vatService)
    : IReportBlock
{
    public string Key => "vat";
    public string Title => Messages.Report_Finance_Vat_Title;
    public int Order => 80;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        (await LoadAsync(period, cancellationToken)).Count > 0;

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var lines = await LoadAsync(period, cancellationToken);
        var rates = await vatService.GetRatesAsync(cancellationToken);

        var groups = new[] { RevenueKind.Ticket, RevenueKind.Combo }
            .Select(kind =>
            {
                var rate = kind == RevenueKind.Ticket ? rates.TicketPercent : rates.ComboPercent;
                var splits = lines
                    .Where(l => l.Kind == kind)
                    .Select(l => VatCalculator.Split(l.Net, rate))
                    .ToList();
                return new
                {
                    Kind = kind,
                    Rate = rate,
                    Gross = splits.Sum(s => s.GrossAmount),
                    Net = splits.Sum(s => s.NetAmount),
                    Tax = splits.Sum(s => s.TaxAmount),
                    Count = splits.Count,
                };
            })
            .Where(g => g.Count > 0)
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("lineType", Messages.Report_Finance_Col_VatLineType, ReportColumnType.Text),
            new("rate", Messages.Report_Finance_Col_VatRate, ReportColumnType.Percent),
            new("lines", Messages.Report_Finance_Col_VatLines, ReportColumnType.Number),
            new("gross", Messages.Report_Finance_Col_VatGross, ReportColumnType.Money),
            new("net", Messages.Report_Finance_Col_VatNet, ReportColumnType.Money),
            new("tax", Messages.Report_Finance_Col_VatTax, ReportColumnType.Money),
        ];

        var rows = groups
            .Select(g =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["lineType"] =
                            g.Kind == RevenueKind.Ticket
                                ? Messages.Report_Finance_Vat_LineTicket
                                : Messages.Report_Finance_Col_Combo,
                        ["rate"] = g.Rate,
                        ["lines"] = g.Count,
                        ["gross"] = g.Gross,
                        ["net"] = g.Net,
                        ["tax"] = g.Tax,
                    }
            )
            .ToList();

        var totals = new Dictionary<string, object?>
        {
            ["lineType"] = Messages.Export_Total,
            ["rate"] = null,
            ["lines"] = groups.Sum(g => g.Count),
            ["gross"] = groups.Sum(g => g.Gross),
            ["net"] = groups.Sum(g => g.Net),
            ["tax"] = groups.Sum(g => g.Tax),
        };

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            totals,
            [Messages.Report_Finance_Vat_GiftCardNote]
        );
    }

    private async Task<List<RecognizedLine>> LoadAsync(
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
        return lines.Where(l => l.Kind != RevenueKind.GiftCard && l.Net > 0m).ToList();
    }
}
