namespace Cinema.Application.DTOs.Orders;

public record OrderListItemDto(
    int OrderId,
    string OrderCode,
    string Type,
    string Channel,
    string Status,
    string? CustomerName,
    string? CustomerPhone,
    string? MovieTitle,
    DateTime? StartTime,
    int TicketCount,
    decimal FinalAmount,
    DateTime CreatedAt,
    DateTime? ExpiresAt
);

public record OrderEmployeeBriefDto(int EmployeeId, string FullName);

public record OrderTicketDto(
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    string? PosterUrl,
    string RoomName,
    DateTime StartTime,
    string SeatRow,
    int SeatNumber,
    int? TicketTypeId,
    string? QrCodeImage,
    bool IsCheckedIn,
    DateTime? CheckedInAt
);

public record OrderComboDto(int? ComboId, string? Name, int QuantityFulfilled);

public record OrderLineDto(
    int OrderLineId,
    string LineType,
    string? Description,
    int Quantity,
    decimal UnitListPrice,
    decimal LockedUnitPrice,
    decimal LineTotal,
    decimal PaidAmount,
    decimal RefundedAmount,
    int RefundedQuantity,
    OrderTicketDto? Ticket,
    OrderComboDto? Combo
);

public record OrderPriceAdjustmentDto(
    int AdjustmentId,
    string Type,
    decimal Amount,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceName
);

public record OrderTransactionDto(
    int TransactionId,
    string Type,
    string Method,
    string Status,
    decimal Amount,
    decimal? GatewayFee,
    string TransactionCode,
    string? GatewayTransactionId,
    string? GatewayResponseCode,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    decimal? AmountReceived,
    decimal? ChangeGiven,
    int? OriginalTransactionId,
    bool NeedsManualRefund,
    string? Note
);

public record OrderPaymentSummaryDto(
    decimal PaidAmount,
    decimal RefundedAmount,
    decimal PendingRefundAmount,
    decimal NetPaidAmount,
    decimal OutstandingAmount,
    decimal OverpaidAmount,
    bool IsFullyPaid
);

public record OrderDetailDto(
    int OrderId,
    string OrderCode,
    string Type,
    string Channel,
    string Status,
    int? UserId,
    string? CustomerName,
    string? CustomerPhone,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    OrderEmployeeBriefDto? CreatedByEmployee,
    int? OriginalOrderId,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    OrderPaymentSummaryDto Payment,
    string? CancelReason,
    string? CancelledBy,
    DateTime? CancelledAt,
    List<OrderLineDto> Lines,
    List<OrderPriceAdjustmentDto> PriceAdjustments,
    List<OrderTransactionDto> Transactions
);
