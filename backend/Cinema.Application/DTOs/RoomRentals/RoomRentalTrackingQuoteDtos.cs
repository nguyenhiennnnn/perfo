using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.RoomRentals;

public record RoomRentalTrackingQuoteRequest(string Token);

public record RoomRentalTrackingQuoteDeclineRequest(string Token, string? Reason);

public record RoomRentalTrackingQuoteDto(
    int VersionNumber,
    bool IsLatest,
    bool CanRespond,
    string RoomName,
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
    DateTime SentAt
);

public record RoomRentalTrackingQuotesDto(
    string RequestCode,
    RoomRentalRequestStatus Status,
    List<RoomRentalTrackingQuoteDto> Versions
);

public record RoomRentalTrackingQuoteDecisionDto(
    string RequestCode,
    RoomRentalRequestStatus Status,
    int VersionNumber,
    DateTime UpdatedAt
);
