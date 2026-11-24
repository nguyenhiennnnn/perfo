namespace Cinema.Application.DTOs.Dashboard;

public record ScheduleReportRowDto(
    int MovieId,
    string? MovieTitle,
    int RoomId,
    string? RoomName,
    string TimeSlot,
    int ShowtimeCount,
    int SellableSeats,
    int TicketsSold,
    decimal? OccupancyPercent,
    decimal NetRevenue,
    decimal RevenuePerShowtime,
    decimal? RevenuePerSeatHour
);

public record ScheduleReportDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ScheduleReportRowDto> Items
);
