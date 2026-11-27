namespace Cinema.Application.DTOs.Reports;

public record RetentionMonthDto(
    int MonthOffset,
    int ReturnedCustomers,
    decimal? ReturnRatePercent,
    bool IsComplete
);

public record RetentionCohortDto(
    DateOnly CohortMonth,
    int CohortSize,
    IReadOnlyList<RetentionMonthDto> Months
);

public record RetentionReportDto(
    IReadOnlyList<RetentionCohortDto> Cohorts,
    DateTime? UpdatedAt
);
