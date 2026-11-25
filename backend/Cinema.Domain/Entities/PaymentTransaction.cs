using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class PaymentTransaction
{
    public int PaymentTransactionId { get; set; }
    public int OrderId { get; set; }
    public PaymentTransactionType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal GatewayFee { get; set; }
    public string TransactionCode { get; set; } = null!;
    public string? GatewayTransactionId { get; set; }
    public string? GatewayResponseCode { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? PerformedByUserId { get; set; }
    public decimal? AmountReceived { get; set; }
    public decimal? ChangeGiven { get; set; }
    public int? OriginalTransactionId { get; set; }
    public int? CounterShiftId { get; set; }
    public int? GiftCardId { get; set; }
    public bool NeedsManualRefund { get; set; }
    public string? Note { get; set; }

    public Order Order { get; set; } = null!;
    public User? PerformedByUser { get; set; }
    public CounterShift? CounterShift { get; set; }
    public GiftCard? GiftCard { get; set; }
    public PaymentTransaction? OriginalTransaction { get; set; }
    public ICollection<PaymentTransaction> Refunds { get; set; } = [];
}
