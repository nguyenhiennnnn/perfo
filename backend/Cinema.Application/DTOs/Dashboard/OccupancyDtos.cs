using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Dashboard;

public enum OccupancyGroupBy
{
    Room,
    TimeSlot,
    Movie,
    Weekday,
}

public record OccupancyGroupDto(
    string Key,
    string? Name,
    int ShowtimeCount,
    int SellableSeats,
    int TicketsSold,
    decimal? OccupancyPercent,
    int PreviousShowtimeCount,
    int PreviousSellableSeats,
    int PreviousTicketsSold,
    decimal? PreviousOccupancyPercent,
    decimal? ChangePoints
);

public record OccupancyTotalDto(
    int ShowtimeCount,
    int SellableSeats,
    int TicketsSold,
    decimal? OccupancyPercent,
    int PreviousShowtimeCount,
    int PreviousSellableSeats,
    int PreviousTicketsSold,
    decimal? PreviousOccupancyPercent,
    decimal? ChangePoints
);

public record OccupancyAnalyticsDto(
    DateOnly From,
    DateOnly To,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    OccupancyGroupBy GroupBy,
    IReadOnlyList<ShowtimeKind> Kinds,
    OccupancyTotalDto Total,
    IReadOnlyList<OccupancyGroupDto> Items
);
