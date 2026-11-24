namespace Cinema.Application.DTOs.Dashboard;

public record SeatHeatmapSeatDto(
    int SeatId,
    string SeatRow,
    int SeatNumber,
    int? SeatTypeId,
    string? SeatTypeName,
    bool IsActive,
    Guid? CoupleId,
    bool IsCouple,
    int TimesSold,
    int TimesAbandoned
);

public record SeatHeatmapDto(
    int RoomId,
    string RoomName,
    DateOnly From,
    DateOnly To,
    int MaxTimesSold,
    int MaxTimesAbandoned,
    List<SeatHeatmapSeatDto> Seats
);
