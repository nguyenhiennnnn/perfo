using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class LoyaltyChallenge
{
    public int ChallengeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public LoyaltyChallengeConditionType ConditionType { get; set; }
    public int TargetCount { get; set; }
    public LoyaltyChallengeRewardType RewardType { get; set; }
    public int? RewardPoints { get; set; }
    public int? RewardGiftId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public LoyaltyReward? RewardGift { get; set; }
    public ICollection<LoyaltyChallengeProgress> Progresses { get; set; } = [];
}
