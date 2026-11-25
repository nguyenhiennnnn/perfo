using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class LoyaltyReward
{
    public int RewardId { get; set; }
    public string Name { get; set; } = null!;
    public LoyaltyRewardType Type { get; set; }
    public int PointsCost { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int CodeValidityDays { get; set; }
    public PromotionDiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? ComboId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Combo? Combo { get; set; }
}
