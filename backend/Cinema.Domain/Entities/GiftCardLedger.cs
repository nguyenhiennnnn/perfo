using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class GiftCardLedger
{
    public long GiftCardLedgerId { get; set; }
    public int GiftCardId { get; set; }
    public GiftCardLedgerType EntryType { get; set; }
    public decimal BalanceChange { get; set; }
    public decimal ReservedChange { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal ReservedAfter { get; set; }
    public int? OrderId { get; set; }
    public DateTime OccurredAt { get; set; }

    public GiftCard GiftCard { get; set; } = null!;
    public Order? Order { get; set; }
}
