namespace Cinema.Application.DTOs.Payments;

public record CashRefundListItemDto(
    int RefundTransactionId,
    string TransactionCode,
    int OrderId,
    string OrderCode,
    int? CustomerUserId,
    string CustomerName,
    string? CustomerPhone,
    bool IsGuest,
    decimal Amount,
    int? ShowtimeId,
    string? MovieTitle,
    DateTime? ShowtimeStartTime,
    string Status,
    DateTime CreatedAt,
    DateTime? RefundedAt,
    int? ConfirmedByUserId,
    string? ConfirmedByName
);

public record CashRefundConfirmResultDto(
    int RefundTransactionId,
    int OrderId,
    string OrderCode,
    decimal Amount,
    string Status,
    DateTime? RefundedAt,
    int? ConfirmedByUserId,
    string? ConfirmedByName
);
