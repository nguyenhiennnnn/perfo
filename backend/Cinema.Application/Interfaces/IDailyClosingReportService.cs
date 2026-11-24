using Cinema.Application.DTOs.DailyClosings;

namespace Cinema.Application.Interfaces;

public interface IDailyClosingReportService
{
    Task<DailyClosingSnapshot> BuildDraftAsync(DateOnly businessDate, CancellationToken cancellationToken);

    Task<DailyClosingSnapshot> BuildSnapshotAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken
    );
}
