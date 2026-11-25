using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class ExceptionalRefundByProposerBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "exceptional-refunds-by-proposer";
    public string Title => Messages.Report_Finance_ExceptionalRefundProposer_Title;
    public int Order => 102;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);
        return await ExceptionalRefundReportSupport
            .ApprovedInWindow(db, window.FromUtc, window.ToUtc)
            .AnyAsync(cancellationToken);
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);

        var groups = (
            await ExceptionalRefundReportSupport
                .ApprovedInWindow(db, window.FromUtc, window.ToUtc)
                .Select(p => new
                {
                    p.ProposedByUserId,
                    Name = p.ProposedByUser.FullName,
                    p.Amount,
                })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(p => p.ProposedByUserId)
            .Select(g => new
            {
                Name = g.First().Name,
                Count = g.Count(),
                Amount = g.Sum(p => p.Amount),
            })
            .OrderByDescending(g => g.Amount)
            .ThenBy(g => g.Name, StringComparer.CurrentCulture)
            .ToList();

        var totalCount = groups.Sum(g => g.Count);
        var totalAmount = groups.Sum(g => g.Amount);

        var rows = groups
            .Select(g => ToRow(g.Name, g.Count, g.Amount, totalAmount))
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("proposer", Messages.Report_Finance_Col_RefundProposer, ReportColumnType.Text),
            new("proposals", Messages.Report_Finance_Col_RefundProposalCount, ReportColumnType.Number),
            new("amount", Messages.Report_Finance_Col_ExceptionalRefundAmount, ReportColumnType.Money),
            new("share", Messages.Report_Finance_Col_ExceptionalRefundShare, ReportColumnType.Percent),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(Messages.Export_Total, totalCount, totalAmount, totalAmount),
            [Messages.Report_Finance_ExceptionalRefund_Note]
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string proposer,
        int count,
        decimal amount,
        decimal totalAmount
    ) =>
        new Dictionary<string, object?>
        {
            ["proposer"] = proposer,
            ["proposals"] = count,
            ["amount"] = amount,
            ["share"] = ReportMath.Percent(amount, totalAmount),
        };
}
