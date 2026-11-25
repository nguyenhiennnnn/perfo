using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class ExceptionalRefundByReasonBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "exceptional-refunds-by-reason";
    public string Title => Messages.Report_Finance_ExceptionalRefundReason_Title;
    public int Order => 101;

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

        var proposals = await ExceptionalRefundReportSupport
            .ApprovedInWindow(db, window.FromUtc, window.ToUtc)
            .OrderBy(p => p.ReviewedAt)
            .ThenBy(p => p.ExceptionalRefundProposalId)
            .Select(p => new { p.Reason, p.Amount })
            .ToListAsync(cancellationToken);

        var groups = proposals
            .GroupBy(p => Normalize(p.Reason), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Reason = g.Key,
                Count = g.Count(),
                Amount = g.Sum(p => p.Amount),
            })
            .OrderByDescending(g => g.Amount)
            .ThenBy(g => g.Reason, StringComparer.CurrentCulture)
            .ToList();

        var totalCount = groups.Sum(g => g.Count);
        var totalAmount = groups.Sum(g => g.Amount);

        var rows = groups
            .Select(g => ToRow(g.Reason, g.Count, g.Amount, totalAmount))
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("reason", Messages.Report_Finance_Col_RefundReason, ReportColumnType.Text),
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

    private static string Normalize(string reason) =>
        string.Join(' ', reason.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IReadOnlyDictionary<string, object?> ToRow(
        string reason,
        int count,
        decimal amount,
        decimal totalAmount
    ) =>
        new Dictionary<string, object?>
        {
            ["reason"] = reason,
            ["proposals"] = count,
            ["amount"] = amount,
            ["share"] = ReportMath.Percent(amount, totalAmount),
        };
}
