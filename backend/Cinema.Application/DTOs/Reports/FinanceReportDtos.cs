namespace Cinema.Application.DTOs.Reports;

public readonly record struct ReportPeriod(DateOnly From, DateOnly To);

public enum ReportColumnType
{
    Text,
    Number,
    Money,
    Percent,
}

public record ReportColumn(string Key, string Title, ReportColumnType Type);

public record ReportBlockResult(
    string Key,
    string Title,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    IReadOnlyDictionary<string, object?>? Totals
);

public record ReportBlockInfoDto(string Key, string Title, int Order);

public record FinanceReportDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ReportBlockResult> Blocks
);
