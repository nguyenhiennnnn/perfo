using Cinema.Application.DTOs.DailyClosings;

namespace Cinema.Application.Interfaces.DailyClosings;

public interface IDailyClosingLedgerReportService
{
    Task<DailyClosingReportDto> GetAsync(
        DateOnly businessDate,
        DailyClosingReportMode mode,
        CancellationToken cancellationToken
    );
}
