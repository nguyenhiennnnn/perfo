namespace Cinema.Application.DTOs.Payments;

public record AdminPaymentListItemDto(
    int TransactionId,
    string TransactionCode,
    int OrderId,
    string OrderCode,
    string Type,
    string Method,
    string Status,
    decimal Amount,
    decimal GatewayFee,
    string? GatewayTransactionId,
    string? GatewayResponseCode,
    decimal? AmountReceived,
    decimal? ChangeGiven,
    int? OriginalTransactionId,
    bool NeedsManualRefund,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record AdminPaymentOrderBriefDto(
    int OrderId,
    string OrderCode,
    string Type,
    string Channel,
    string Status,
    int? UserId,
    string? CustomerName,
    string? CustomerPhone,
    string? MovieTitle,
    DateTime? StartTime,
    int TicketCount,
    decimal FinalAmount,
    DateTime CreatedAt,
    string? CancelReason
);

public record AdminPaymentRelatedTransactionDto(
    int TransactionId,
    string TransactionCode,
    string Type,
    string Method,
    string Status,
    decimal Amount,
    bool NeedsManualRefund,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record AdminPaymentRefundSummaryDto(
    int ChargeTransactionId,
    decimal ChargeAmount,
    decimal RefundedAmount,
    decimal PendingRefundAmount,
    decimal RefundableAmount
);

public record AdminPaymentDetailDto(
    int TransactionId,
    string TransactionCode,
    int OrderId,
    string Type,
    string Method,
    string Status,
    decimal Amount,
    decimal GatewayFee,
    string? GatewayTransactionId,
    string? GatewayResponseCode,
    decimal? AmountReceived,
    decimal? ChangeGiven,
    bool NeedsManualRefund,
    string? Note,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int? PerformedByUserId,
    string? PerformedByName,
    AdminPaymentRelatedTransactionDto? OriginalTransaction,
    List<AdminPaymentRelatedTransactionDto> RelatedRefunds,
    AdminPaymentRefundSummaryDto? RefundSummary,
    AdminPaymentOrderBriefDto Order
);
