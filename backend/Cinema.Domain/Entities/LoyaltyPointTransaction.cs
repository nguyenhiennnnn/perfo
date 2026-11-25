using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class LoyaltyPointTransaction
{
    public long TransactionId { get; set; }
    public int UserId { get; set; }
    public int? EntryId { get; set; }
    public int? OrderId { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public int Points { get; set; }
    public int? ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public LoyaltyPointEntry? Entry { get; set; }
    public Order? Order { get; set; }
}
