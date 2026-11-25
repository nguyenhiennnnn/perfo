using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class GiftCardRevenueEntry
{
    public long GiftCardRevenueEntryId { get; set; }
    public int GiftCardId { get; set; }
    public int OrderId { get; set; }
    public int PaymentTransactionId { get; set; }
    public long? RecognizedEntryId { get; set; }
    public GiftCardRevenueEntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime OccurredAt { get; set; }

    public GiftCard GiftCard { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public PaymentTransaction PaymentTransaction { get; set; } = null!;
    public GiftCardRevenueEntry? RecognizedEntry { get; set; }
}
