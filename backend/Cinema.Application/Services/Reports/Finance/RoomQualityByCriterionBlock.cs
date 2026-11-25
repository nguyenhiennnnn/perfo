using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class RoomQualityByCriterionBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    private const int LowRatingMax = 2;

    public string Key => "room-quality-by-criterion";
    public string Title => Messages.Report_RoomQuality_ByCriterion_Title;
    public int Order => 111;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        RoomQualityReportSupport.AnyAsync(db, cache, period, cancellationToken);

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var surveys = await RoomQualityReportSupport.LoadAsync(db, cache, period, cancellationToken);

        var criteria = new (string Name, Func<RoomQualitySurveyRow, int> Rating)[]
        {
            (Messages.Report_RoomQuality_Criterion_Room, s => s.Room),
            (Messages.Report_RoomQuality_Criterion_Sound, s => s.Sound),
            (Messages.Report_RoomQuality_Criterion_Picture, s => s.Picture),
            (Messages.Report_RoomQuality_Criterion_Cleanliness, s => s.Cleanliness),
            (Messages.Report_RoomQuality_Criterion_CounterService, s => s.CounterService),
        };

        var rows = criteria
            .Select(c =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["criterion"] = c.Name,
                        ["surveys"] = surveys.Count,
                        ["average"] = RoomQualityReportSupport.Average(surveys, s => c.Rating(s)),
                        ["lowRate"] = ReportMath.Percent(
                            surveys.Count(s => c.Rating(s) <= LowRatingMax),
                            surveys.Count
                        ),
                    }
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("criterion", Messages.Report_RoomQuality_Col_Criterion, ReportColumnType.Text),
            new("surveys", Messages.Report_RoomQuality_Col_Surveys, ReportColumnType.Number),
            new("average", Messages.Report_RoomQuality_Col_Average, ReportColumnType.Number),
            new("lowRate", Messages.Report_RoomQuality_Col_LowRate, ReportColumnType.Percent),
        ];

        return new ReportBlockResult(Key, Title, columns, rows, null);
    }
}
