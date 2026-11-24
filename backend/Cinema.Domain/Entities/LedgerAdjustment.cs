using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class LedgerAdjustment
{
    public int LedgerAdjustmentId { get; set; }
    public DateOnly PostingDate { get; set; }
    public DateOnly OriginalDate { get; set; }
    public LedgerAdjustmentType Type { get; set; }
    public decimal TicketRevenueDelta { get; set; }
    public decimal ComboRevenueDelta { get; set; }
    public decimal GiftCardSoldDelta { get; set; }
    public decimal DiscountDelta { get; set; }
    public decimal RefundAmountDelta { get; set; }
    public int CancelledOrdersDelta { get; set; }
    public string Reason { get; set; } = null!;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? OrderId { get; set; }
    public int? PaymentTransactionId { get; set; }
    public string? SourceKey { get; set; }

    public User? CreatedByUser { get; set; }
    public Order? Order { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
}
