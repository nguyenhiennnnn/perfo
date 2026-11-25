using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class SupportRequestReportBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    private sealed record RequestLine(
        SupportRequestType Type,
        SupportRequestStatus Status,
        DateTime CreatedAt,
        DateTime DueAt,
        DateTime? ResolvedAt
    );

    private sealed record TypeStats(
        int Requests,
        int Resolved,
        int ResolvedOnTime,
        int OverdueOpen,
        decimal? AverageHours
    );

    public string Key => "support-requests";
    public string Title => Messages.Report_Finance_Support_Title;
    public int Order => 100;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);
        return await db
            .SupportRequests.AsNoTracking()
            .AnyAsync(
                r => r.CreatedAt >= window.FromUtc && r.CreatedAt < window.ToUtc,
                cancellationToken
            );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);

        var lines = await db
            .SupportRequests.AsNoTracking()
            .Where(r => r.CreatedAt >= window.FromUtc && r.CreatedAt < window.ToUtc)
            .Select(r => new RequestLine(r.Type, r.Status, r.CreatedAt, r.DueAt, r.ResolvedAt))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var byType = lines
            .GroupBy(l => l.Type)
            .ToDictionary(g => g.Key, g => Summarize(g.ToList(), now));

        var rows = Enum.GetValues<SupportRequestType>()
            .Where(byType.ContainsKey)
            .Select(type => ToRow(TypeLabel(type), byType[type]))
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("type", Messages.Report_Finance_Col_SupportType, ReportColumnType.Text),
            new("requests", Messages.Report_Finance_Col_SupportRequests, ReportColumnType.Number),
            new("resolved", Messages.Report_Finance_Col_SupportResolved, ReportColumnType.Number),
            new("resolvedOnTime", Messages.Report_Finance_Col_SupportOnTime, ReportColumnType.Number),
            new("overdueOpen", Messages.Report_Finance_Col_SupportOverdueOpen, ReportColumnType.Number),
            new("averageHours", Messages.Report_Finance_Col_SupportAverageHours, ReportColumnType.Number),
            new("onTimeRate", Messages.Report_Finance_Col_SupportOnTimeRate, ReportColumnType.Percent),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(Messages.Export_Total, Summarize(lines, now)),
            [Messages.Report_Finance_Support_Note]
        );
    }

    private static TypeStats Summarize(IReadOnlyCollection<RequestLine> lines, DateTime now)
    {
        var resolved = lines
            .Where(l => l.Status == SupportRequestStatus.Resolved && l.ResolvedAt is not null)
            .ToList();
        var onTime = resolved.Count(l => l.ResolvedAt <= l.DueAt);
        var overdueOpen = lines.Count(l =>
            l.Status != SupportRequestStatus.Resolved && l.DueAt < now
        );

        decimal? averageHours = null;
        if (resolved.Count > 0)
        {
            var ticks = resolved.Sum(l => Math.Max((l.ResolvedAt!.Value - l.CreatedAt).Ticks, 0L));
            averageHours = Math.Round(
                ticks / (decimal)TimeSpan.TicksPerHour / resolved.Count,
                2,
                MidpointRounding.AwayFromZero
            );
        }

        return new TypeStats(lines.Count, resolved.Count, onTime, overdueOpen, averageHours);
    }

    private static IReadOnlyDictionary<string, object?> ToRow(string label, TypeStats stats) =>
        new Dictionary<string, object?>
        {
            ["type"] = label,
            ["requests"] = stats.Requests,
            ["resolved"] = stats.Resolved,
            ["resolvedOnTime"] = stats.ResolvedOnTime,
            ["overdueOpen"] = stats.OverdueOpen,
            ["averageHours"] = stats.AverageHours,
            ["onTimeRate"] = ReportMath.Percent(
                stats.ResolvedOnTime,
                stats.Resolved + stats.OverdueOpen
            ),
        };

    private static string TypeLabel(SupportRequestType type) =>
        type switch
        {
            SupportRequestType.TicketEmailNotReceived =>
                Messages.Report_Finance_SupportType_TicketEmailNotReceived,
            SupportRequestType.ChargedOrderNotConfirmed =>
                Messages.Report_Finance_SupportType_ChargedOrderNotConfirmed,
            SupportRequestType.RefundOutsidePolicy =>
                Messages.Report_Finance_SupportType_RefundOutsidePolicy,
            SupportRequestType.ShowtimeFeedback =>
                Messages.Report_Finance_SupportType_ShowtimeFeedback,
            _ => Messages.Report_Finance_SupportType_Other,
        };
}
