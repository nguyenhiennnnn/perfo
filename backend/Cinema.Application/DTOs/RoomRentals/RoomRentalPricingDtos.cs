using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.RoomRentals;

public record RoomRentalRateDto(
    int RoomRentalRateId,
    string Name,
    int RoomId,
    string RoomName,
    string StartTime,
    string EndTime,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    decimal BasePrice,
    decimal PerPersonFee,
    int IncludedAttendees,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertRoomRentalRateRequest(
    string Name,
    int RoomId,
    string StartTime,
    string EndTime,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    decimal BasePrice,
    decimal PerPersonFee,
    int IncludedAttendees,
    bool IsActive = true
);

public record RoomRentalRateListFilter(string? Search, int? RoomId, bool? IsActive);

public record RoomRentalQuoteComboItem(int ComboId, int Quantity);

public record RoomRentalQuoteExtraItem(string Label, decimal Amount, int Quantity = 1);

public record RoomRentalQuoteRequest(
    int RoomId,
    DateTime StartAt,
    int DurationMinutes,
    int AttendeeCount,
    List<RoomRentalQuoteComboItem>? Combos,
    List<RoomRentalQuoteExtraItem>? Extras,
    decimal? DiscountAmount,
    decimal? DiscountPercent,
    string? DiscountReason
);

public record RoomRentalQuoteLineDto(
    RoomRentalQuoteLineKind Kind,
    string Label,
    int Quantity,
    decimal UnitPrice,
    decimal Amount,
    int? ReferenceId
);

public record RoomRentalQuoteResultDto(
    int RoomId,
    string RoomName,
    int RoomRentalRateId,
    string RoomRentalRateName,
    DateTime StartAt,
    int DurationMinutes,
    int AttendeeCount,
    List<RoomRentalQuoteLineDto> Lines,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal Total
);
