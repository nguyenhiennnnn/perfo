using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class PromotionRedemption
{
    public long PromotionRedemptionId { get; set; }
    public int PromotionId { get; set; }
    public int OrderId { get; set; }
    public int? UserId { get; set; }
    public PromotionRedemptionStatus Status { get; set; } = PromotionRedemptionStatus.Reserved;
    public decimal DiscountAmount { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public User? User { get; set; }
}
