using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Retention;

public class RetentionReportService(IAppDbContext db) : IRetentionReportService
{
    private const int RateDecimals = 2;

    public async Task<RetentionReportDto> GetAsync(int months, CancellationToken cancellationToken)
    {
        months =
            months <= 0
                ? RetentionRules.DefaultReportMonths
                : Math.Min(months, RetentionRules.MaxReportMonths);
        var now = DateTime.UtcNow;
        var latest = RetentionRules.MonthOf(now);
        var from = latest.AddMonths(-(months - 1));

        var rows = await db
            .CustomerRetentionStats.AsNoTracking()
            .Where(s => s.CohortMonth >= from && s.CohortMonth <= latest)
            .OrderBy(s => s.CohortMonth)
            .ThenBy(s => s.MonthOffset)
            .ToListAsync(cancellationToken);

        var cohorts = rows.GroupBy(s => s.CohortMonth)
            .OrderByDescending(g => g.Key)
            .Select(g => new RetentionCohortDto(
                g.Key,
                g.First().CohortSize,
                g.Select(s => new RetentionMonthDto(
                        s.MonthOffset,
                        s.ReturnedCustomers,
                        s.CohortSize > 0
                            ? Math.Round(s.ReturnedCustomers * 100m / s.CohortSize, RateDecimals)
                            : null,
                        RetentionRules.IsComplete(g.Key.AddMonths(s.MonthOffset), now)
                    ))
                    .ToList()
            ))
            .ToList();

        return new RetentionReportDto(cohorts, rows.Count == 0 ? null : rows.Max(s => s.UpdatedAt));
    }
}
