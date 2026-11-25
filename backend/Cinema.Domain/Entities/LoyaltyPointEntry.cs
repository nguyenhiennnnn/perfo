using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class LoyaltyPointEntry
{
    public int EntryId { get; set; }
    public int UserId { get; set; }
    public int? OrderId { get; set; }
    public LoyaltyLotType Type { get; set; } = LoyaltyLotType.Earn;
    public int Points { get; set; }
    public int RevokedPoints { get; set; }
    public int RemainingPoints { get; set; }
    public DateTime EarnedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ExpiryReminderSentAt { get; set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
}
