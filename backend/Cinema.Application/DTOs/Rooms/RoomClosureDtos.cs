namespace Cinema.Application.DTOs.Rooms;

public record RoomClosureDto(
    int RoomClosureId,
    int RoomId,
    string RoomName,
    DateTime StartTime,
    DateTime EndTime,
    string Reason,
    int? CreatedByUserId,
    DateTime CreatedAt
);

public record CreateRoomClosureRequest(
    int RoomId,
    DateTime StartTime,
    DateTime EndTime,
    string Reason
);

public record RoomClosureListFilter(int? RoomId, DateTime? From, DateTime? To, bool? Ongoing);

public record DeleteRoomClosureResponse(int RoomClosureId, bool Deleted, bool EndedEarly);

public record AffectedShowtimeDto(
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    int RoomId,
    string RoomName,
    DateTime StartTime,
    DateTime EndTime,
    int OrderCount,
    int CustomerCount,
    int TicketCount
);

public record AffectedShowtimesResponse(
    int RoomId,
    DateTime StartTime,
    DateTime EndTime,
    List<AffectedShowtimeDto> Showtimes,
    int TotalShowtimes,
    int TotalOrders,
    int TotalCustomers,
    int TotalTickets
);
