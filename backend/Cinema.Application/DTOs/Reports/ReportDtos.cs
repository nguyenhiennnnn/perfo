namespace Cinema.Application.DTOs.Reports;

public record RevenueReportItemDto(
    string Period,
    decimal TicketRevenue,
    decimal ComboRevenue,
    decimal TotalRevenue,
    int BookingCount
);

public record RevenueSummaryDto(decimal TotalRevenue, int TotalBookings);

public record RevenueReportDto(
    string GroupBy,
    List<RevenueReportItemDto> Items,
    RevenueSummaryDto Summary
);

public record TopMovieDto(int MovieId, string Title, int TicketsSold, decimal Revenue);

public record EmployeePerformanceDto(
    int EmployeeId,
    string FullName,
    int TicketsSold,
    decimal Revenue,
    int BookingCount
);

public record ComboReportItemDto(
    int ComboId,
    string ComboName,
    int QuantitySold,
    int QuantityDelivered,
    int QuantityPending,
    int QuantitySoldWithTickets,
    int QuantitySoldStandalone,
    decimal Revenue
);

public record ComboReportSummaryDto(
    int QuantitySold,
    int QuantityDelivered,
    int QuantityPending,
    decimal Revenue
);

public record ComboReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    List<ComboReportItemDto> Items,
    ComboReportSummaryDto Summary
);
