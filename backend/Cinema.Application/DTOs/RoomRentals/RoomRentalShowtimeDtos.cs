namespace Cinema.Application.DTOs.RoomRentals;

public record RoomRentalShowtimeDto(
    int ShowtimeId,
    int RoomId,
    DateTime StartTime,
    DateTime EndTime,
    string? ContentTitle,
    int BlockedSeatCount
);
