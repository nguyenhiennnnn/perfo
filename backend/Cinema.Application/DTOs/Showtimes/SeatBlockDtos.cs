namespace Cinema.Application.DTOs.Showtimes;

public record SeatBlockDto(
    int SeatBlockId,
    int ShowtimeId,
    int ShowtimeSeatId,
    string SeatRow,
    int SeatNumber,
    string Type,
    string Reason,
    DateTime? ExpiresAt,
    int? CreatedByUserId,
    DateTime CreatedAt
);

public record BlockSeatsRequest(
    List<int> ShowtimeSeatIds,
    string Type,
    string Reason,
    DateTime? ExpiresAt
);

public record UnblockSeatsRequest(List<int> ShowtimeSeatIds);

public record UnblockSeatsResponse(int ShowtimeId, List<int> ShowtimeSeatIds);
