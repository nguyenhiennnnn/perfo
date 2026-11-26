namespace Cinema.Application.DTOs.RoomRentals;

public record RoomRentalCancellationTierDto(int MinHoursBeforeStart, int RefundPercent);

public record UpsertRoomRentalQuoteVersionRequest(
    RoomRentalQuoteRequest Pricing,
    DateTime? ValidUntil,
    decimal? DepositPercent,
    DateTime? BalanceDueAt,
    List<RoomRentalCancellationTierDto>? CancellationTiers,
    string? Note
);

public record RoomRentalQuoteVersionListItemDto(
    int RoomRentalQuoteId,
    int VersionNumber,
    bool IsCurrent,
    decimal Total,
    decimal DepositAmount,
    DateTime ValidUntil,
    DateTime? SentAt,
    DateTime CreatedAt
);

public record RoomRentalQuoteVersionDto(
    int RoomRentalQuoteId,
    int RoomRentalRequestId,
    string RequestCode,
    int VersionNumber,
    bool IsCurrent,
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
    decimal Total,
    decimal DepositPercent,
    decimal DepositAmount,
    decimal BalanceAmount,
    DateTime ValidUntil,
    DateTime BalanceDueAt,
    List<RoomRentalCancellationTierDto> CancellationTiers,
    string? Note,
    DateTime? SentAt,
    string? SentToEmail,
    DateTime CreatedAt
);
