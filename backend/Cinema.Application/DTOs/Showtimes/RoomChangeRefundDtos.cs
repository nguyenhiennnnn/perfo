namespace Cinema.Application.DTOs.Showtimes;

public record RoomChangeRefundListItemDto(
    int RoomChangeRefundDueId,
    int ShowtimeRoomChangeId,
    int ShowtimeId,
    string MovieTitle,
    DateTime ShowtimeStartTime,
    int OrderId,
    string OrderCode,
    int? CustomerUserId,
    string CustomerName,
    string? CustomerPhone,
    bool IsGuest,
    int OrderLineId,
    string FromSeat,
    string ToSeat,
    decimal AmountDue,
    string Status,
    DateTime CreatedAt
);

public record RoomChangeRefundTransactionDto(
    int RefundTransactionId,
    string Method,
    decimal Amount,
    string Status
);

public record RoomChangeRefundConfirmResultDto(
    int RoomChangeRefundDueId,
    int OrderId,
    string OrderCode,
    decimal Amount,
    string Status,
    bool AlreadyRefunded,
    IReadOnlyList<RoomChangeRefundTransactionDto> Transactions
);
