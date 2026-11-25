using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class GiftCard
{
    public int GiftCardId { get; set; }
    public string Code { get; set; } = null!;
    public GiftCardType Type { get; set; } = GiftCardType.Gift;
    public decimal FaceValue { get; set; }
    public decimal Balance { get; set; }
    public decimal ReservedAmount { get; set; }
    public int PurchaserUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? Message { get; set; }
    public DateTime? SendAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public GiftCardStatus Status { get; set; } = GiftCardStatus.PendingSend;
    public DateTime CreatedAt { get; set; }
    public uint RowVersion { get; set; }

    public User PurchaserUser { get; set; } = null!;
    public ICollection<GiftCardLedger> LedgerEntries { get; set; } = [];
}
