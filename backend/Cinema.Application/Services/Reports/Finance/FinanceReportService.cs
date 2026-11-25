using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class FinanceReportService(IEnumerable<IReportBlock> registeredBlocks) : IFinanceReportService
{
    private const int MaxDays = 366;

    private readonly List<IReportBlock> _blocks = registeredBlocks
        .OrderBy(b => b.Order)
        .ThenBy(b => b.Key, StringComparer.Ordinal)
        .ToList();

    public IReadOnlyList<ReportBlockInfoDto> GetBlocks() =>
        _blocks.Select(b => new ReportBlockInfoDto(b.Key, b.Title, b.Order)).ToList();

    public async Task<FinanceReportDto> GetAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<string>? blocks,
        CancellationToken cancellationToken
    )
    {
        if (from == default || to == default || from > to)
            throw new BusinessRuleException(Messages.Rule_OccupancyRangeInvalid);
        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_OccupancyRangeTooLong, MaxDays)
            );

        var selected = _blocks;
        var requested = (blocks ?? [])
            .SelectMany(b => b.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count > 0)
        {
            var unknown = requested
                .Where(k => !_blocks.Any(b => string.Equals(b.Key, k, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (unknown.Count > 0)
                throw new ValidationAppException(
                    string.Format(Messages.Invalid_ReportBlockNotFound, string.Join(", ", unknown))
                );
            selected = _blocks
                .Where(b => requested.Contains(b.Key, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var period = new ReportPeriod(from, to);
        var results = new List<ReportBlockResult>();
        foreach (var block in selected)
        {
            if (!await block.HasDataAsync(period, cancellationToken))
                continue;
            results.Add(await block.BuildAsync(period, cancellationToken));
        }

        return new FinanceReportDto(from, to, results);
    }
}
