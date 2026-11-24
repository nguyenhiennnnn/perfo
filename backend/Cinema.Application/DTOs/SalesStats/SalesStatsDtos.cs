namespace Cinema.Application.DTOs.SalesStats;

public record RebuildSalesStatsRequest(DateOnly FromDate, DateOnly ToDate);

public record SalesStatsRebuildParameters(string FromDate, string ToDate);
