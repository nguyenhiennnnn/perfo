using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class RoomQualityByRoomBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "room-quality-by-room";
    public string Title => Messages.Report_RoomQuality_ByRoom_Title;
    public int Order => 110;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        RoomQualityReportSupport.AnyAsync(db, cache, period, cancellationToken);

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var surveys = await RoomQualityReportSupport.LoadAsync(db, cache, period, cancellationToken);

        var rows = surveys
            .GroupBy(s => new { s.RoomId, s.RoomName })
            .OrderBy(g => g.Key.RoomName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(g => g.Key.RoomId)
            .Select(g =>
                ToRow(
                    g.Key.RoomName,
                    g.Count(),
                    RoomQualityReportSupport.Average(g, s => s.Room),
                    RoomQualityReportSupport.Average(g, s => s.Sound),
                    RoomQualityReportSupport.Average(g, s => s.Picture),
                    RoomQualityReportSupport.Average(g, s => s.Cleanliness),
                    RoomQualityReportSupport.Average(g, s => s.RoomScore)
                )
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("room", Messages.Report_RoomQuality_Col_Room, ReportColumnType.Text),
            new("surveys", Messages.Report_RoomQuality_Col_Surveys, ReportColumnType.Number),
            new("roomRating", Messages.Report_RoomQuality_Criterion_Room, ReportColumnType.Number),
            new("soundRating", Messages.Report_RoomQuality_Criterion_Sound, ReportColumnType.Number),
            new("pictureRating", Messages.Report_RoomQuality_Criterion_Picture, ReportColumnType.Number),
            new("cleanlinessRating", Messages.Report_RoomQuality_Criterion_Cleanliness, ReportColumnType.Number),
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
                RoomQualityReportSupport.Average(surveys, s => s.RoomScore)
            ),
            [Messages.Report_RoomQuality_ScoreNote]
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string room,
        int surveys,
        decimal roomRating,
        decimal soundRating,
        decimal pictureRating,
        decimal cleanlinessRating,
        decimal score
    ) =>
        new Dictionary<string, object?>
        {
            ["room"] = room,
            ["surveys"] = surveys,
            ["roomRating"] = roomRating,
            ["soundRating"] = soundRating,
            ["pictureRating"] = pictureRating,
            ["cleanlinessRating"] = cleanlinessRating,
            ["score"] = score,
        };
}
