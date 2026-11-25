using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ReferralRelation
{
    public int ReferralId { get; set; }
    public int ReferrerUserId { get; set; }
    public int ReferredUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RewardGrantedAt { get; set; }
    public DateTime? RewardBlockedAt { get; set; }
    public ReferralBlockReason? RewardBlockedReason { get; set; }

    public User Referrer { get; set; } = null!;
    public User Referred { get; set; } = null!;
}
