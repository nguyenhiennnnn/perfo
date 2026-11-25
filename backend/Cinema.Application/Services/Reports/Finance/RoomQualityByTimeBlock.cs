using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class RoomQualityByTimeBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "room-quality-by-time";
    public string Title => Messages.Report_RoomQuality_ByTime_Title;
    public int Order => 112;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        RoomQualityReportSupport.AnyAsync(db, cache, period, cancellationToken);

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var surveys = await RoomQualityReportSupport.LoadAsync(db, cache, period, cancellationToken);

        var rows = surveys
            .GroupBy(s => s.BusinessDate)
            .OrderBy(g => g.Key)
            .Select(g =>
                ToRow(
                    g.Key.ToString(DateTimePatterns.Date),
                    g.Count(),
                    RoomQualityReportSupport.Average(g, s => s.Room),
                    RoomQualityReportSupport.Average(g, s => s.Sound),
                    RoomQualityReportSupport.Average(g, s => s.Picture),
                    RoomQualityReportSupport.Average(g, s => s.Cleanliness),
                    RoomQualityReportSupport.Average(g, s => s.CounterService),
                    RoomQualityReportSupport.Average(g, s => s.RoomScore)
                )
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("date", Messages.Report_Finance_Col_Date, ReportColumnType.Text),
            new("surveys", Messages.Report_RoomQuality_Col_Surveys, ReportColumnType.Number),
            new("roomRating", Messages.Report_RoomQuality_Criterion_Room, ReportColumnType.Number),
            new("soundRating", Messages.Report_RoomQuality_Criterion_Sound, ReportColumnType.Number),
            new("pictureRating", Messages.Report_RoomQuality_Criterion_Picture, ReportColumnType.Number),
            new("cleanlinessRating", Messages.Report_RoomQuality_Criterion_Cleanliness, ReportColumnType.Number),
            new("counterServiceRating", Messages.Report_RoomQuality_Criterion_CounterService, ReportColumnType.Number),
            new("score", Messages.Report_RoomQuality_Col_Score, ReportColumnType.Number),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(
                Messages.Export_Total,
                surveys.Count,
                RoomQualityReportSupport.Average(surveys, s => s.Room),
                RoomQualityReportSupport.Average(surveys, s => s.Sound),
                RoomQualityReportSupport.Average(surveys, s => s.Picture),
                RoomQualityReportSupport.Average(surveys, s => s.Cleanliness),
                RoomQualityReportSupport.Average(surveys, s => s.CounterService),
                RoomQualityReportSupport.Average(surveys, s => s.RoomScore)
            ),
            [Messages.Report_RoomQuality_ScoreNote]
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string date,
        int surveys,
        decimal room,
        decimal sound,
        decimal picture,
        decimal cleanliness,
        decimal counterService,
        decimal score
    ) =>
        new Dictionary<string, object?>
        {
            ["date"] = date,
            ["surveys"] = surveys,
            ["roomRating"] = room,
            ["soundRating"] = sound,
            ["pictureRating"] = picture,
            ["cleanlinessRating"] = cleanliness,
            ["counterServiceRating"] = counterService,
            ["score"] = score,
        };
}
