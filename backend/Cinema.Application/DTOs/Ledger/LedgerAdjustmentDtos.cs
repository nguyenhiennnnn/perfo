using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Ledger;

public sealed record LedgerAdjustmentInput(
    DateOnly OriginalDate,
    LedgerAdjustmentType Type,
    string Reason,
    decimal TicketRevenueDelta = 0m,
    decimal ComboRevenueDelta = 0m,
    decimal GiftCardSoldDelta = 0m,
    decimal DiscountDelta = 0m,
    decimal RefundAmountDelta = 0m,
    int CancelledOrdersDelta = 0,
    int? OrderId = null,
    int? PaymentTransactionId = null,
    string? SourceKey = null,
    int? CreatedByUserId = null
)
{
    public bool IsEmpty =>
        TicketRevenueDelta == 0m
        && ComboRevenueDelta == 0m
        && GiftCardSoldDelta == 0m
        && DiscountDelta == 0m
        && RefundAmountDelta == 0m
        && CancelledOrdersDelta == 0;

    public LedgerAdjustment ToEntity(DateOnly postingDate, DateTime createdAt) =>
        new()
        {
            PostingDate = postingDate,
            OriginalDate = OriginalDate,
            Type = Type,
            TicketRevenueDelta = TicketRevenueDelta,
            ComboRevenueDelta = ComboRevenueDelta,
            GiftCardSoldDelta = GiftCardSoldDelta,
            DiscountDelta = DiscountDelta,
            RefundAmountDelta = RefundAmountDelta,
            CancelledOrdersDelta = CancelledOrdersDelta,
            Reason = Reason,
            OrderId = OrderId,
            PaymentTransactionId = PaymentTransactionId,
            SourceKey = SourceKey,
            CreatedByUserId = CreatedByUserId,
            CreatedAt = createdAt,
        };
}

public record CreateLedgerAdjustmentRequest(
    DateOnly OriginalDate,
    string Reason,
    decimal TicketRevenueDelta = 0m,
    decimal ComboRevenueDelta = 0m,
    decimal GiftCardSoldDelta = 0m,
    decimal DiscountDelta = 0m,
    decimal RefundAmountDelta = 0m,
    int CancelledOrdersDelta = 0,
    int? OrderId = null,
    int? PaymentTransactionId = null
);

public record LedgerAdjustmentListFilter(
    DateOnly? PostingFrom,
    DateOnly? PostingTo,
    DateOnly? OriginalDate,
    string? Type
);

public record LedgerAdjustmentDto(
    int LedgerAdjustmentId,
    DateOnly PostingDate,
    DateOnly OriginalDate,
    string Type,
    decimal TicketRevenueDelta,
    decimal ComboRevenueDelta,
    decimal GiftCardSoldDelta,
    decimal DiscountDelta,
    decimal RefundAmountDelta,
    int CancelledOrdersDelta,
    string Reason,
    int? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    int? OrderId,
    int? PaymentTransactionId
);
