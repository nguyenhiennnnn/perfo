using Cinema.Application.DTOs.DailyClosings;

namespace Cinema.Application.Interfaces;

public interface IDailyClosingLedgerReportService
{
    Task<DailyClosingReportDto> GetAsync(
        DateOnly businessDate,
        DailyClosingReportMode mode,
        CancellationToken cancellationToken
    );
}
