namespace Cinema.Application.DTOs.Showtimes;

public record RoomChangeSeatOverrideDto(int ShowtimeSeatId, int TargetSeatId);

public record ShowtimeRoomChangeRequest(
    int TargetRoomId,
    List<RoomChangeSeatOverrideDto>? Overrides = null,
    string? Note = null
);

public record RoomChangeSeatRefDto(
    string Row,
    int Number,
    int? SeatTypeId,
    string? SeatType,
    bool IsCouple
);

public record RoomChangeTargetSeatDto(
    int SeatId,
    string Row,
    int Number,
    int? SeatTypeId,
    string? SeatType,
    bool IsCouple,
    Guid? CoupleId
);

public record RoomChangeSeatMappingDto(
    int ShowtimeSeatId,
    string Status,
    bool HasOrders,
    RoomChangeSeatRefDto Source,
    int? TargetSeatId,
    RoomChangeSeatRefDto? Target,
    string MatchKind,
    bool NeedsReview,
    decimal RefundDue
);

public record RoomChangePreviewResponse(
    int ShowtimeId,
    int SourceRoomId,
    string SourceRoomName,
    int TargetRoomId,
    string TargetRoomName,
    bool CanApply,
    IReadOnlyList<string> Issues,
    int OccupiedSeatCount,
    int TargetSeatCount,
    int NeedsReviewCount,
    int RefundDueCount,
    decimal RefundDueTotal,
    IReadOnlyList<RoomChangeSeatMappingDto> Mappings,
    IReadOnlyList<RoomChangeTargetSeatDto> TargetSeats
);

public record ShowtimeRoomChangeResponse(
    int ShowtimeRoomChangeId,
    int ShowtimeId,
    int FromRoomId,
    string FromRoomName,
    int ToRoomId,
    string ToRoomName,
    int SeatsMoved,
    int OrderCount,
    int RefundDueCount,
    decimal RefundDueTotal
);
